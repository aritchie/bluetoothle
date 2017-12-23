using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly DeviceContext context;


        public GattCharacteristic(DeviceContext context,
                                  Native native,
                                  IGattService service)
                            : base(service,
                                   native.Uuid,
                                   (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.context = context;
            this.Native = native;
        }


        byte[] value;
        public override byte[] Value => this.value;
        public Native Native { get; }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(async ob =>
            {
                var result = await this.Native.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                //if (result.Status != GattCommunicationStatus.Success)
                foreach (var dnative in result.Descriptors)
                {
                    var descriptor = new GattDescriptor(dnative, this);
                    ob.OnNext(descriptor);
                }
                return Disposable.Empty;
            })
            .Replay();
            return this.descriptorOb;
        }


        // TODO: reliable write
        public override IObservable<CharacteristicGattResult> Write(byte[] value) => Observable.FromAsync(async ct =>
        {
            this.AssertWrite(false);
            var result = await this.Native
                .WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithResponse)
                .AsTask(ct)
                .ConfigureAwait(false);

            this.context.Ping();
            if (result != GattCommunicationStatus.Success)
                return this.ToResult(GattEvent.WriteError, "Error writing characteristic - " + result);

            this.value = value;
            return this.ToResult(GattEvent.Write, value);
         });


        public override IObservable<CharacteristicGattResult> Read() => Observable.FromAsync(async ct =>
        {
            this.AssertRead();
            var result = await this.Native
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            this.context.Ping();
            if (result.Status != GattCommunicationStatus.Success)
                return this.ToResult(GattEvent.ReadError, "Error reading characteristics - " + result.Status);

            this.value = result.Value.ToArray();
            return this.ToResult(GattEvent.Read, this.value);
        });


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationIfAvailable)
        {
            var type = useIndicationIfAvailable && this.CanIndicate()
                ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                : GattClientCharacteristicConfigurationDescriptorValue.Notify;

            return this.SetNotify(type);
        }


        public override IObservable<CharacteristicGattResult> DisableNotifications() =>
            this.SetNotify(GattClientCharacteristicConfigurationDescriptorValue.None);


        IObservable<CharacteristicGattResult> SetNotify(GattClientCharacteristicConfigurationDescriptorValue value)
            => Observable.FromAsync(async ct =>
            {

                var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(value);
                if (status != GattCommunicationStatus.Success)
                {
                    this.context.SetNotifyCharacteristic(this.Native, value != GattClientCharacteristicConfigurationDescriptorValue.None);
                    return this.ToResult(GattEvent.Notification, "");
                }
                return this.ToResult(GattEvent.NotificationError, status.ToString());
            });


        IObservable<CharacteristicGattResult> notificationOb;
        public override IObservable<CharacteristicGattResult> WhenNotificationReceived()
        {
            this.AssertNotify();

            this.notificationOb = this.notificationOb ?? Observable.Create<CharacteristicGattResult>(ob =>
            {
                //var trigger = new GattCharacteristicNotificationTrigger(this.native);

                var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
                {
                    if (sender.Equals(this.Native))
                    {
                        var bytes = args.CharacteristicValue.ToArray();
                        var result = this.ToResult(GattEvent.Notification, bytes);
                        ob.OnNext(result);
                    }
                });
                this.Native.ValueChanged += handler;

                return () => this.Native.ValueChanged -= handler;
            });
            return this.notificationOb;
        }


        public override async void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            await this.Native.WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithoutResponse);
            this.value = value;
        }

    }
}
