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


        public GattCharacteristic(DeviceContext context, Native native, IGattService service)
            : base(service, native.Uuid, (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.context = context;
            this.Native = native;
        }


        public Native Native { get; }

        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(async ob =>
            {
                var result = await this.Native.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                //if (result.Status)
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


        public override IObservable<CharacteristicResult> Read()
        {
            this.AssertRead();

            return Observable.FromAsync(async ct =>
            {
                var result = await this.Native
                    .ReadValueAsync(BluetoothCacheMode.Uncached)
                    .AsTask(ct);

                this.context.Ping();
                if (result.Status != GattCommunicationStatus.Success)
                    throw new Exception("Error reading characteristics - " + result.Status);

                var bytes = result.Value.ToArray();
                this.Value = bytes;
                var r = new CharacteristicResult(this, CharacteristicEvent.Read, bytes);

                return r;
            });
        }


        public override IObservable<bool> EnableNotifications(bool useIndicationIfAvailable)
        {
            var type = useIndicationIfAvailable && this.CanIndicate()
                ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                : GattClientCharacteristicConfigurationDescriptorValue.Notify;

            return this
                .SetNotify(type)
                .Select(x => x != null);
        }


        public override IObservable<object> DisableNotifications() =>
            this.SetNotify(GattClientCharacteristicConfigurationDescriptorValue.None);


        IObservable<object> SetNotify(GattClientCharacteristicConfigurationDescriptorValue value)
            => Observable.FromAsync(async ct =>
            {
                this.AssertNotify();
                var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(value);
                if (status == GattCommunicationStatus.Success)
                {
                    this.context.SetNotifyCharacteristic(this.Native, value != GattClientCharacteristicConfigurationDescriptorValue.None);
                    return new object();
                }
                return null;
            });


        IObservable<CharacteristicResult> notificationOb;
        public override IObservable<CharacteristicResult> WhenNotificationReceived()
        {
            this.notificationOb = this.notificationOb ?? Observable.Create<CharacteristicResult>(ob =>
            {
                //var trigger = new GattCharacteristicNotificationTrigger(this.native);

                var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
                {
                    if (sender.Equals(this.Native))
                    {
                        var bytes = args.CharacteristicValue.ToArray();
                        var result = new CharacteristicResult(this, CharacteristicEvent.Notification, bytes);
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
            this.Value = value;
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            // TODO: reliable write
            this.AssertWrite(false);

            return Observable.FromAsync(async ct =>
            {
                var result = await this.Native
                    .WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithResponse)
                    .AsTask(ct);

                this.context.Ping();
                if (result != GattCommunicationStatus.Success)
                    throw new Exception("Error writing characteristic");

                this.Value = value;
                var r = new CharacteristicResult(this, CharacteristicEvent.Write, value);
                return r;
            });
        }
    }
}
