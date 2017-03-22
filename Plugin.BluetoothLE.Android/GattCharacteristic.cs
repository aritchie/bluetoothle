using System;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Android.Bluetooth;
using Java.Util;
using Plugin.BluetoothLE.Internals;
using Observable = System.Reactive.Linq.Observable;



namespace Plugin.BluetoothLE
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
                    if (this.Equals(args.Characteristic))
                    {
                        Log.Write("Incoming Characteristic Write Event - " + args.Characteristic.Uuid);

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

                if (this.Properties.HasFlag(CharacteristicProperties.Write))
                {
                    Log.Write("Hooking for write response - " + this.Uuid);
                    this.context.Callbacks.CharacteristicWrite += handler;
                    this.RawWriteWithResponse(value);
                }
                else
                {
                    Log.Write("Write with No Response - " + this.Uuid);
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
                    if (this.Equals(args.Characteristic))
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
                this.context.Callbacks.CharacteristicRead += handler;
                this.context.Gatt.ReadCharacteristic(this.native);

                return () => this.context.Callbacks.CharacteristicRead -= handler;
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
                    if (this.Equals(args.Characteristic))
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
                AndroidConfig.SyncPost(() =>
                {
                    descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                    success = this.context.Gatt.WriteDescriptor(descriptor);
                    if (success)
                        this.IsNotifying = true;
                });
            }
            return success;
        }


        protected virtual void DisableNotifications()
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Characteristic Client Configuration Descriptor not found");

            AndroidConfig.SyncPost(() =>
            {
                descriptor.SetValue(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
                this.context.Gatt.WriteDescriptor(descriptor);
                this.context.Gatt.SetCharacteristicNotification(this.native, false);
                this.IsNotifying = false;
            });
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


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => this.Uuid.ToString();


        bool Equals(GattCharacteristicEventArgs args)
        {
            if (this.native.Equals(args.Characteristic))
                return true;

            if (!this.native.Uuid.Equals(args.Characteristic.Uuid))
                return false;

            if (!this.native.Service.Uuid.Equals(args.Characteristic.Service.Uuid))
                return false;

            if (!this.context.Gatt.Equals(args.Gatt))
                return false;

            return true;
        }


        void RawWriteWithResponse(byte[] bytes)
        {
            AndroidConfig.SyncPost(() =>
            {
                this.native.SetValue(bytes);
                this.native.WriteType = GattWriteType.Default;
                this.context.Gatt.WriteCharacteristic(this.native);
            });
        }


        void RawWriteNoResponse(IObserver<CharacteristicResult> ob, byte[] bytes)
        {
            var result = new CharacteristicResult(this, CharacteristicEvent.Write, bytes);

            AndroidConfig.SyncPost(() =>
            {
                this.native.SetValue(bytes);
                this.native.WriteType = GattWriteType.NoResponse;
                this.context.Gatt.WriteCharacteristic(this.native);
                this.Value = bytes;
            });
            this.WriteSubject.OnNext(result);
            ob?.Respond(result);
        }
    }
}