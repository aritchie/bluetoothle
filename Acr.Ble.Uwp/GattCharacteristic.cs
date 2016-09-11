using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Acr.Ble
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly Native native;


        public GattCharacteristic(Native native, IGattService service) : base(service, native.Uuid, (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.native = native;
        }


        public override IObservable<byte[]> Read()
        {
            return Observable.Create<byte[]>(async ob =>
            {
                var result = await this.native.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Error reading characteristics - " + result.Status));
                }
                else
                {
                    var bytes = result.Value.ToArray();
                    ob.Respond(bytes);
                }
                return Disposable.Empty;
            });
        }


        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            throw new NotImplementedException();
        }


        public override IObservable<object> Write(byte[] value)
        {
            return Observable.Create<byte[]>(async ob =>
            {
                var result = await this.native.WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithResponse);

                if (result == GattCommunicationStatus.Success)
                {
                    ob.Respond(null);
                }
                else
                {
                    ob.OnError(new Exception("Error writing characteristic"));
                }
                return Disposable.Empty;
            });
        }


        IObservable<byte[]> notificationOb;
        public override IObservable<byte[]> WhenNotificationOccurs()
        {
            this.notificationOb = this.notificationOb ?? Observable.Create<byte[]>(async ob =>
            {
                var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
                {
                    if (sender.Equals(this.native))
                    {
                        var bytes = args.CharacteristicValue.ToArray();
                        ob.OnNext(bytes);
                    }
                });
                this.native.ValueChanged += handler;

                var status = await this.native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Could not subscribe to notifications"));
                }

                return () =>
                {
                    this.native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).GetResults();
                    this.native.ValueChanged -= handler;
                };
            });
            return this.notificationOb;
        }
    }
}
