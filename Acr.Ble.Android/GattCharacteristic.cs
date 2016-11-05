using System;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Acr.Ble;
using Android.Bluetooth;
using Acr.Ble.Internals;
using Java.Util;
using Observable = System.Reactive.Linq.Observable;


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
            this.native.SetValue(value);
            this.native.WriteType = GattWriteType.NoResponse;
            this.context.Gatt.WriteCharacteristic(this.native);
            this.Value = value;
            this.WriteSubject.OnNext(this.Value);
        }


        public override IObservable<object> Write(byte[] value)
        {
            this.AssertWrite(false);

            return Observable.Create<object>(ob =>
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
                            ob.Respond(this.Value);
                            this.WriteSubject.OnNext(this.Value);
                        }
                    }
                });
                this.context.Callbacks.CharacteristicWrite += handler;
                this.native.SetValue(value);

                if (this.Properties.HasFlag(CharacteristicProperties.Write))
                {
                    this.native.WriteType = GattWriteType.Default;
                    this.context.Gatt.WriteCharacteristic(this.native);
                }
                else
                {
                    this.native.WriteType = GattWriteType.NoResponse;
                    this.context.Gatt.WriteCharacteristic(this.native);
                    this.Value = value;
                    ob.Respond(this.Value);
                    this.WriteSubject.OnNext(this.Value);
                }
                return () => this.context.Callbacks.CharacteristicWrite -= handler;
            });
        }


        public override IObservable<byte[]> Read()
        {
            this.AssertRead();

            return Observable.Create<byte[]>(ob =>
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
                            ob.Respond(this.Value);
                            this.ReadSubject.OnNext(this.Value);
                        }
                    }
                });
                this.context.Callbacks.CharacteristicRead += handler;
                this.context.Gatt.ReadCharacteristic(this.native);
                return () => this.context.Callbacks.CharacteristicRead -= handler;
            });
        }


        IObservable<byte[]> notifyOb;
        public override IObservable<byte[]> SubscribeToNotifications()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<byte[]>(ob =>
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
                            ob.OnNext(this.Value);
                            this.NotifySubject.OnNext(this.Value);
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
    }
}