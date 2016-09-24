using System;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Android.Bluetooth;
using Acr.Ble.Internals;


namespace Acr.Ble
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        static readonly Java.Util.UUID NotifyDescriptorId = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        static readonly byte[] Empty = { 0x0, 0x0 };
        readonly BluetoothGattCharacteristic native;
        readonly GattContext context;


        public GattCharacteristic(IGattService service, GattContext context, BluetoothGattCharacteristic native) : base(service, native.Uuid.ToGuid(), (CharacteristicProperties)(int)native.Properties)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<object> Write(byte[] value)
        {
            this.AssertWrite();

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
                this.EnableNotifications(true);
                this.context.Callbacks.CharacteristicChanged += handler;

                return () =>
                {
                    this.EnableNotifications(false);
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


        protected virtual bool EnableNotifications(bool enable)
        {
            if (!this.CanNotify())
                return false;

            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);

            if (!enable)
            {
                descriptor.SetValue(Empty);
                this.context.Gatt.WriteDescriptor(descriptor);
                this.context.Gatt.SetCharacteristicNotification(this.native, false);
                this.IsNotifying = false;
                return true;
            }

            var success = false;
            if (descriptor != null)
            {
                success = this.context.Gatt.SetCharacteristicNotification(this.native, enable);
                Thread.Sleep(100);

                if (success)
                {
                    descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                    success = this.context.Gatt.WriteDescriptor(descriptor);
                    if (success)
                        this.IsNotifying = true;
                }
            }
            return success;
        }
    }
}