using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
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
        readonly DeviceContext context;


        public GattCharacteristic(IGattService service,
                                  DeviceContext context,
                                  BluetoothGattCharacteristic native)
                            : base(service,
                                   native.Uuid.ToGuid(),
                                   (CharacteristicProperties)(int)native.Properties)
        {
            this.context = context;
            this.native = native;
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            this
                .RawWriteNoResponse(null, value)
                .Subscribe();
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value) => this.context.Lock(Observable.Create<CharacteristicGattResult>(async ob =>
        {
            this.AssertWrite(false);

            var sub = this.context
                .Callbacks
                .CharacteristicWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    Log.Debug("Characteristic", "write event - " + args.Characteristic.Uuid);

                    CharacteristicGattResult result = null;
                    if (!args.IsSuccessful)
                    {
                        result = new CharacteristicGattResult(
                            this,
                            GattEvent.WriteError,
                            $"Failed to write characteristic - {args.Status}"
                        );
                    }
                    else
                    {
                        this.Value = value;
                        result = new CharacteristicGattResult(
                            this,
                            GattEvent.Write,
                            this.Value
                        );
                    }
                    this.WriteSubject.OnNext(result);
                    ob.Respond(result);
                });

            if (this.Properties.HasFlag(CharacteristicProperties.Write))
            {
                Log.Debug("Characteristic", "Hooking for write response - " + this.Uuid);
                await this.context.Marshall(() =>
                {
                    this.native.SetValue(value);
                    this.native.WriteType = GattWriteType.Default;
                    if (!this.context.Gatt.WriteCharacteristic(this.native))
                        ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to write to characteristic"));
                });
            }
            else
            {
                Log.Debug("Characteristic", "Write with No Response - " + this.Uuid);
                await this.RawWriteNoResponse(ob, value);
            }
            return sub;
        }));


        public override IObservable<CharacteristicGattResult> Read() => this.context.Lock(Observable.Create<CharacteristicGattResult>(async ob =>
        {
            this.AssertRead();

            var sub = this.context
                .Callbacks
                .CharacteristicRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (!args.IsSuccessful)
                    {
                        ob.OnNext(this.ToResult(
                            GattEvent.ReadError,
                            $"Failed to read characteristic - {args.Status}"
                        ));
                    }
                    else
                    {
                        this.Value = args.Characteristic.GetValue();
                        var result = this.ToResult(GattEvent.Read, this.Value);
                        this.ReadSubject.OnNext(result);
                        ob.Respond(result);
                    }
                });

            await this.context.Marshall(() =>
            {
                if (!this.context.Gatt.ReadCharacteristic(this.native))
                {
                    ob.Respond(this.ToResult(
                        GattEvent.ReadError,
                        "Failed to read characteristic"
                    ));
                }
            });

            return sub;
        }));


        // this should not be placed in a lock - let it fall to the descriptor
        public override IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationsIfAvailable) => Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                return this.ToResult(GattEvent.NotificationError, "Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, true))
                return this.ToResult(GattEvent.NotificationError, "Failed to set characteristic notification value");

            if (CrossBleAdapter.AndroidOperationPause != null)
                await Task.Delay(CrossBleAdapter.AndroidOperationPause.Value, ct);

            var bytes = useIndicationsIfAvailable && this.CanIndicate()
                ? BluetoothGattDescriptor.EnableIndicationValue.ToArray()
                : BluetoothGattDescriptor.EnableNotificationValue.ToArray();

            // TODO
            var result = await wrap.Write(bytes);
            this.IsNotifying = true;

            // TODO
            return null;
        });


        //// this should not be placed in a lock - let it fall to the descriptor
        public override IObservable<CharacteristicGattResult> DisableNotifications() => Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                return this.ToResult(GattEvent.NotificationError, "Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, false))
                return this.ToResult(GattEvent.NotificationError, "Could not set characteristic value");

            if (CrossBleAdapter.AndroidOperationPause != null)
                await Task.Delay(CrossBleAdapter.AndroidOperationPause.Value, ct);

            // TODO
            var result = await wrap.Write(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
            this.IsNotifying = false;

            return null;
        });


        IObservable<CharacteristicGattResult> notifyOb;
        public override IObservable<CharacteristicGattResult> WhenNotificationReceived()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<CharacteristicGattResult>(ob =>
                this.context
                    .Callbacks
                    .CharacteristicChanged
                    .Where(this.NativeEquals)
                    .Subscribe(args =>
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnNext(this.ToResult(
                                GattEvent.NotificationError,
                                "Error subscribing to " + args.Status.ToString()
                            ));
                        }
                        else
                        {
                            this.Value = args.Characteristic.GetValue();
                            ob.OnNext(this.ToResult(
                                GattEvent.Notification,
                                this.Value
                            ));
                        }
                    })
            )
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


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
            if (other == null)
                return false;

            if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => $"Characteristic: {this.Uuid}";


        bool NativeEquals(GattCharacteristicEventArgs args)
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


        IObservable<Unit> RawWriteNoResponse(IObserver<CharacteristicGattResult> ob, byte[] bytes) => this.context.Marshall(() =>
        {
            this.native.SetValue(bytes);
            this.native.WriteType = GattWriteType.NoResponse;
            if (!this.context.Gatt.WriteCharacteristic(this.native))
            {
                ob?.Respond(this.ToResult(GattEvent.WriteError, "Failed to write to characteristic"));
            }
            else
            {
                this.Value = bytes;
                var result = this.ToResult(GattEvent.Write, bytes);
                this.WriteSubject.OnNext(result);
                ob?.Respond(result);
            }
        });
    }
}