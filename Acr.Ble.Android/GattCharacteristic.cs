using System;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Android.Bluetooth;
using Acr.Ble.Internals;
using Java.Util;
using Observable = System.Reactive.Linq.Observable;
using Android.App;


namespace Acr.Ble
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        static readonly UUID NotifyDescriptorId = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        readonly BluetoothGattCharacteristic native;
        readonly GattContext context;


        public GattCharacteristic(IGattService service, GattContext context, BluetoothGattCharacteristic native) : base(service, native.Uuid.ToGuid(), (CharacteristicProperties)(int)native.Properties)
        {
            this.context = context;
            this.native = native;
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            this.RawWriteNoResponse(null, value);
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            this.AssertWrite(false);

            return Observable.Create<CharacteristicResult>(ob =>
            {
                var handler = new EventHandler<GattCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.Uuid.Equals(this.native.Uuid))
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnError(new ArgumentException($"Failed to write characteristic - {args.Status}"));
                        }
                        else
                        {
                            this.Value = value;
                            var result = new CharacteristicResult(this, CharacteristicEvent.Write, this.Value);
                            ob.Respond(result);
                            this.WriteSubject.OnNext(result);
                        }
                    }
                });
                this.context.Callbacks.CharacteristicWrite += handler;

                if (this.Properties.HasFlag(CharacteristicProperties.Write))
                {
                    this.RawWriteWithResponse(value);
                }
                else
                {
                    this.RawWriteNoResponse(ob, value);
                }
                return () => this.context.Callbacks.CharacteristicWrite -= handler;
            });
        }


        public override IObservable<CharacteristicResult> Read()
        {
            this.AssertRead();

            return Observable.Create<CharacteristicResult>(ob =>
            {
                

                var handler = new EventHandler<GattCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.Uuid.Equals(this.native.Uuid))
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnError(new ArgumentException($"Failed to read characteristic - {args.Status}"));
                        }
                        else
                        {
                            this.Value = args.Characteristic.GetValue();

                            var result = new CharacteristicResult(this, CharacteristicEvent.Read, this.Value);
                            ob.Respond(result);
                            this.ReadSubject.OnNext(result);
                        }
                    }
                });
                //var cancelSrc = new CancellationTokenSource();
                //await this.context.ReadWriteLock.WaitAsync(cancelSrc.Token);
                this.context.Callbacks.CharacteristicRead += handler;
                this.context.Gatt.ReadCharacteristic(this.native);
                //this.context.ReadWriteLock.Release();

                return () => 
                {
                    this.context.Callbacks.CharacteristicRead -= handler;
                    //cancelSrc.Cancel();
                };
            });
        }


        IObservable<CharacteristicResult> notifyOb;
        public override IObservable<CharacteristicResult> SubscribeToNotifications()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<CharacteristicResult>(ob =>
            {
                var handler = new EventHandler<GattCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.Uuid.Equals(this.native.Uuid))
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnError(new ArgumentException("Error subscribing to " + args.Characteristic.Uuid));
                        }
                        else
                        {
                            this.Value = args.Characteristic.GetValue();

                            var result = new CharacteristicResult(this, CharacteristicEvent.Notification, this.Value);
                            ob.OnNext(result);
                            this.NotifySubject.OnNext(result);
                        }
                    }
                });
                this.EnableNotifications();
                this.context.Callbacks.CharacteristicChanged += handler;

                return () =>
                {
                    this.DisableNotifications();
                    this.context.Callbacks.CharacteristicChanged -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.notifyOb;
        }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(ob =>
            {
                foreach (var nd in this.native.Descriptors)
                {
                    var wrap = new GattDescriptor(this, this.context, nd);
                    ob.OnNext(wrap);
                }
                return Disposable.Empty;
            })
            .Replay()
            .RefCount();

            return this.descriptorOb;
        }


        protected virtual bool EnableNotifications()
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Characteristic Client Configuration Descriptor not found");

            var success = this.context.Gatt.SetCharacteristicNotification(this.native, true);
            Thread.Sleep(100);

            if (success)
            {
                descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                success = this.context.Gatt.WriteDescriptor(descriptor);
                if (success)
                    this.IsNotifying = true;
            }
            return success;
        }


        protected virtual void DisableNotifications()
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Characteristic Client Configuration Descriptor not found");

            descriptor.SetValue(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
            this.context.Gatt.WriteDescriptor(descriptor);
            this.context.Gatt.SetCharacteristicNotification(this.native, false);
            this.IsNotifying = false;
        }


        public override int GetHashCode()
        {
            return this.native.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
            if (other == null)
                return false;

            if (!this.native.Equals(other.native))
                return false;

            return true;
        }


        public override string ToString()
        {
            return this.Uuid.ToString();
        }


        void RawWriteWithResponse(byte[] bytes)
        {
            // TODO: sync lock
            this.WrapWrite(() =>
            {
                this.native.SetValue(bytes);
                this.native.WriteType = GattWriteType.Default;
                this.context.Gatt.WriteCharacteristic(this.native);
            });
        }


        void RawWriteNoResponse(IObserver<CharacteristicResult> ob, byte[] bytes)
        {
            // TODO: sync lock
            var result = new CharacteristicResult(this, CharacteristicEvent.Write, bytes);

            this.WrapWrite(() =>
            {
                this.native.SetValue(bytes);            
                this.native.WriteType = GattWriteType.NoResponse;
                this.context.Gatt.WriteCharacteristic(this.native);
                this.Value = bytes;

                // TODO: switch off this thread again?
                this.WriteSubject.OnNext(result);
                if (ob != null)
                    ob.Respond(result);
            });
        }


        void WrapWrite(Action action)
        {
            if (AndroidConfig.WriteOnMainThread)
            {
                Application.SynchronizationContext.Post((state) => action(), null);
            }
            else
            {
                action();
            }            
        }
    }
}