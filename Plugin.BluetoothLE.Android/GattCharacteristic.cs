using System;
using System.Linq;
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
            : base(service, native.Uuid.ToGuid(), (CharacteristicProperties)(int)native.Properties)
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


        public override IObservable<CharacteristicResult> Write(byte[] value) => this.context.Lock(Observable.Create<CharacteristicResult>(async ob =>
        {
            this.AssertWrite(false);

            Log.Debug("Characteristic", "past write gate");
            var sub = this.context
                .Callbacks
                .CharacteristicWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    Log.Debug("Characteristic", "write vent - " + args.Characteristic.Uuid);

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
                });

            if (this.Properties.HasFlag(CharacteristicProperties.Write))
            {
                Log.Debug("Characteristic", "Hooking for write response - " + this.Uuid);
                await this.RawWriteWithResponse(value);
            }
            else
            {
                Log.Debug("Characteristic", "Write with No Response - " + this.Uuid);
                await this.RawWriteNoResponse(ob, value);
            }
            return sub;
        }));


        public override IObservable<CharacteristicResult> Read() => this.context.Lock(Observable.Create<CharacteristicResult>(async ob =>
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
                        ob.OnError(new ArgumentException($"Failed to read characteristic - {args.Status}"));
                    }
                    else
                    {
                        this.Value = args.Characteristic.GetValue();

                        var result = new CharacteristicResult(this, CharacteristicEvent.Read, this.Value);
                        ob.Respond(result);
                        this.ReadSubject.OnNext(result);
                    }
                });

            await this.context.Marshall(() =>
            {
                try
                {
                    this.context.Gatt.ReadCharacteristic(this.native);
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            });

            return sub;
        }));


        // this should not be placed in a lock - let it fall to the descriptor
        public override IObservable<bool> EnableNotifications(bool useIndicationsIfAvailable) => Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            var success = this.context.Gatt.SetCharacteristicNotification(this.native, true);

            if (CrossBleAdapter.AndroidOperationPause != null)
                await Task.Delay(CrossBleAdapter.AndroidOperationPause.Value, ct);

            if (success)
            {
                var bytes = useIndicationsIfAvailable && this.CanIndicate()
                    ? BluetoothGattDescriptor.EnableIndicationValue.ToArray()
                    : BluetoothGattDescriptor.EnableNotificationValue.ToArray();

                await wrap.Write(bytes);
                this.IsNotifying = true;
            }
            return success;
        });


        // this should not be placed in a lock - let it fall to the descriptor
        public override IObservable<object> DisableNotifications() => Observable.FromAsync<object>(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            var success = this.context
                .Gatt
                .SetCharacteristicNotification(this.native, false);

            if (CrossBleAdapter.AndroidOperationPause != null)
                await Task.Delay(CrossBleAdapter.AndroidOperationPause.Value, ct);

            if (success)
            {
                await wrap.Write(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
                this.IsNotifying = false;
            }
            return null;
        });


        IObservable<CharacteristicResult> notifyOb;
        public override IObservable<CharacteristicResult> WhenNotificationReceived()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<CharacteristicResult>(ob =>
                this.context
                    .Callbacks
                    .CharacteristicChanged
                    .Where(this.NativeEquals)
                    .Subscribe(args =>
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
            if (this.context.Gatt == null || args.Characteristic?.Service == null)
                return false;

            if (this.native.Equals(args.Characteristic))
                return true;

            if (!this.native.Uuid.Equals(args.Characteristic.Uuid))
                return false;

            if (!this.native.Service?.Uuid.Equals(args.Characteristic?.Service.Uuid) ?? false)
                return false;

            if (!this.context.Gatt.Equals(args.Gatt))
                return false;

            return true;
        }


        IObservable<object> RawWriteWithResponse(byte[] bytes) => this.context.Marshall(() =>
        {
            this.native.SetValue(bytes);
            this.native.WriteType = GattWriteType.Default;
            this.context.Gatt.WriteCharacteristic(this.native);
        });


        IObservable<object> RawWriteNoResponse(IObserver<CharacteristicResult> ob, byte[] bytes) => this.context.Marshall(() =>
        {
            try
            {
                this.native.SetValue(bytes);
                this.native.WriteType = GattWriteType.NoResponse;
                this.context.Gatt.WriteCharacteristic(this.native);
                this.Value = bytes;

                var result = new CharacteristicResult(this, CharacteristicEvent.Write, bytes);
                this.WriteSubject.OnNext(result);
                ob?.Respond(result);
            }
            catch (Exception ex)
            {
                ob?.OnError(ex);
            }
        });
    }
}