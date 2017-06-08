using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.UI.Core;
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


        public override IObservable<bool> SetNotificationValue(CharacteristicConfigDescriptorValue value)
        {
            this.AssertNotify();

            return Observable.FromAsync(async ct =>
            {
                var tcs = new TaskCompletionSource<GattCommunicationStatus>();
                await CoreWindow
                    .GetForCurrentThread()
                    .Dispatcher
                    .RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            var desc = GetConfigValue(value);
                            var result = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(desc);
                            tcs.TrySetResult(result);
                        }
                    );

                var status = await tcs.Task;
                if (status == GattCommunicationStatus.Success)
                {
                    this.context.SetNotifyCharacteristic(this.Native, value != CharacteristicConfigDescriptorValue.None);
                    return true;
                }
                return false;
            });
        }


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


        static GattClientCharacteristicConfigurationDescriptorValue GetConfigValue(CharacteristicConfigDescriptorValue value)
        {
            switch (value)
            {
                case CharacteristicConfigDescriptorValue.Indicate:
                    return GattClientCharacteristicConfigurationDescriptorValue.Indicate;

                case CharacteristicConfigDescriptorValue.Notify:
                    return GattClientCharacteristicConfigurationDescriptorValue.Notify;

                case CharacteristicConfigDescriptorValue.None:
                    return GattClientCharacteristicConfigurationDescriptorValue.None;

                default:
                    throw new ArgumentException("Invalid characteristic config descriptor value");
            }
        }
    }
}
