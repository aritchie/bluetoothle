using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using Acr.Logging;
using Acr.Reactive;
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


        public override byte[] Value => this.native.GetValue();


        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
            => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertWrite(false);

            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    this.native.WriteType = GattWriteType.NoResponse;

                    if (!this.native.SetValue(value))
                        throw new BleException("Failed to write characteristic value");

                    if (!this.context.Gatt.WriteCharacteristic(this.native))
                        throw new BleException("Failed to write to characteristic");

                    ob.Respond( new CharacteristicGattResult(this, value));
                }
                catch (Exception ex)
                {
                    ob.OnError(new BleException("Error during charactersitic write", ex));
                }
            });

            return Disposable.Empty;
        }));


        public override IObservable<CharacteristicGattResult> Write(byte[] value)
            => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertWrite(false);

            var sub = this.context
                .Callbacks
                .CharacteristicWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    Log.Debug(BleLogCategory.Characteristic, "write event - " + args.Characteristic.Uuid);
                    if (args.IsSuccessful)
                        ob.Respond(new CharacteristicGattResult(this, value));
                    else
                        ob.OnError(new BleException($"Failed to write characteristic - {args.Status}"));
                });

            Log.Debug(BleLogCategory.Characteristic, "Hooking for write response - " + this.Uuid);
            this.context.InvokeOnMainThread(() =>
            {
                this.native.WriteType = GattWriteType.Default;
                this.native.SetValue(value);
                //if (!this.native.SetValue(value))
                    //ob.OnError(new BleException("Failed to set characteristic value"));

                //else if (!this.context.Gatt.WriteCharacteristic(this.native))
                if (!this.context.Gatt?.WriteCharacteristic(this.native) ?? false)
                    ob.OnError(new BleException("Failed to write to characteristic"));
            });

            return sub;
        }));


        public override IObservable<CharacteristicGattResult> Read()
            => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertRead();

            var sub = this.context
                .Callbacks
                .CharacteristicRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                        ob.Respond(new CharacteristicGattResult(this, args.Characteristic.GetValue()));
                    else
                        ob.OnError(new BleException($"Failed to read characteristic - {args.Status}"));
                });

            this.context.InvokeOnMainThread(() =>
            {
                if (!this.context.Gatt?.ReadCharacteristic(this.native) ?? false)
                    ob.OnError(new BleException("Failed to read characteristic"));
            });

            return sub;
        }));


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationsIfAvailable)
            => this.context.Invoke(Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new BleException("Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, true))
                throw new BleException("Failed to set characteristic notification value");

            await this.context.OpPause(ct).ConfigureAwait(false);
            var bytes = this.GetNotifyDescriptorBytes(useIndicationsIfAvailable);

            await wrap
                .WriteInternal(bytes)
                .ToTask(ct)
                .ConfigureAwait(false);

            this.IsNotifying = true;
            return new CharacteristicGattResult(this, null);
        }));


        public override IObservable<CharacteristicGattResult> DisableNotifications()
            => this.context.Invoke(Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new BleException("Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, false))
                throw new BleException("Could not set characteristic notification value");

            await this.context.OpPause(ct).ConfigureAwait(false);
            await wrap
                .WriteInternal(BluetoothGattDescriptor.DisableNotificationValue.ToArray())
                .ToTask(ct)
                .ConfigureAwait(false);

            this.IsNotifying = false;
            return new CharacteristicGattResult(this, null);
        }));


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
                        if (args.IsSuccessful)
                            ob.OnNext(new CharacteristicGattResult(this, args.Characteristic.GetValue()));
                        else
                            ob.OnError(new BleException($"Notification error - {args.Status}"));
                    })
            )
            .Publish()
            .RefCount();

            return this.notifyOb;
        }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> DiscoverDescriptors()
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


        byte[] GetNotifyDescriptorBytes(bool useIndicationsIfAvailable)
        {
            // if only indicate
            if (useIndicationsIfAvailable && this.CanIndicate())
                return BluetoothGattDescriptor.EnableIndicationValue.ToArray();

             return BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        }
    }
}