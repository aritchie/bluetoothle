using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using Acr;
using Acr.Logging;
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
                CharacteristicGattResult result = null;
                try
                {
                    this.native.WriteType = GattWriteType.NoResponse;

                    if (!this.native.SetValue(value))
                        result = this.ToResult(GattEvent.WriteError, "Failed to set characteristic value");

                    else if (this.context.Gatt.WriteCharacteristic(this.native))
                        result = this.ToResult(GattEvent.Write, value);

                    else
                        result = this.ToResult(GattEvent.WriteError, "Failed to write to characteristic");
                }
                catch (Exception ex)
                {
                    result = this.ToResult(GattEvent.WriteError, ex.ToString());
                }
                ob.OnNext(result);
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

                    var result = args.IsSuccessful
                        ? this.ToResult(GattEvent.Write, value)
                        : this.ToResult(GattEvent.WriteError,$"Failed to write characteristic - {args.Status}");

                    ob.Respond(result);
                });

            Log.Debug(BleLogCategory.Characteristic, "Hooking for write response - " + this.Uuid);
            this.context.InvokeOnMainThread(() =>
            {
                this.native.WriteType = GattWriteType.Default;
                if (!this.native.SetValue(value))
                    ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to set characteristic value"));

                else if (!this.context.Gatt.WriteCharacteristic(this.native))
                    ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to write to characteristic"));
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
                    var result = args.IsSuccessful
                        ? this.ToResult(GattEvent.Read, args.Characteristic.GetValue())
                        : this.ToResult(GattEvent.ReadError, $"Failed to read characteristic - {args.Status}");

                    ob.Respond(result);
                });

            this.context.InvokeOnMainThread(() =>
            {
                if (!this.context.Gatt.ReadCharacteristic(this.native))
                    ob.Respond(this.ToResult(GattEvent.ReadError, "Failed to read characteristic"));
            });

            return sub;
        }));


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationsIfAvailable)
            => this.context.Invoke(Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                return this.ToResult(GattEvent.NotificationError, "Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, true))
                return this.ToResult(GattEvent.NotificationError, "Failed to set characteristic notification value");

            await this.context.OpPause(ct).ConfigureAwait(false);
            var bytes = useIndicationsIfAvailable && this.CanIndicate()
                ? BluetoothGattDescriptor.EnableIndicationValue.ToArray()
                : BluetoothGattDescriptor.EnableNotificationValue.ToArray();

            var result = await wrap
                .WriteInternal(bytes)
                .ToTask(ct)
                .ConfigureAwait(false);

            if (!result.Success)
                return this.ToResult(GattEvent.NotificationError, "Failed to set notification descriptor - " + result.ErrorMessage);

            this.IsNotifying = true;
            return this.ToResult(GattEvent.Notification, "");
        }));


        public override IObservable<CharacteristicGattResult> DisableNotifications()
            => this.context.Invoke(Observable.FromAsync(async ct =>
        {
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                return this.ToResult(GattEvent.NotificationError, "Characteristic Client Configuration Descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, false))
                return this.ToResult(GattEvent.NotificationError, "Could not set characteristic value");

            await this.context.OpPause(ct).ConfigureAwait(false);
            var result = await wrap
                .WriteInternal(BluetoothGattDescriptor.DisableNotificationValue.ToArray())
                .ToTask(ct)
                .ConfigureAwait(false);

            if (!result.Success)
                return this.ToResult(GattEvent.NotificationError, "Failed to set notification descriptor - " + result.ErrorMessage);

            this.IsNotifying = true;
            return this.ToResult(GattEvent.Notification, "");
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
                        var result = args.IsSuccessful
                            ? this.ToResult(GattEvent.Notification, args.Characteristic.GetValue())
                            : this.ToResult(GattEvent.NotificationError, "Notification error - " + args.Status.ToString());

                        ob.OnNext(result);
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
    }
}