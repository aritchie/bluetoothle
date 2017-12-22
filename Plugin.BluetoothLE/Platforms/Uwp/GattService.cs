using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly DeviceContext context;
        readonly Native native;


        public GattService(DeviceContext context, Native native) : base(context.Device, native.Uuid, false)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(async ob =>
            {
                foreach (var uuid in characteristicIds)
                {
                    var result = await this.native.GetCharacteristicsForUuidAsync(uuid, BluetoothCacheMode.Cached);
                    if (result.Status != GattCommunicationStatus.Success)
                        throw new ArgumentException("Could not find GATT service - " + result.Status);

                    var ch = new GattCharacteristic(this.context, result.Characteristics.First(), this);
                    ob.OnNext(ch);
                }
                ob.OnCompleted();

                return Disposable.Empty;
            });


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(async ob =>
            {
                var result = await this.native.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                foreach (var characteristic in result.Characteristics)
                {
                    var wrap = new GattCharacteristic(this.context, characteristic, this);
                    ob.OnNext(wrap);
                }
                return Disposable.Empty;
            })
            .Replay()
            .RefCount();

            return this.characteristicOb;
        }
    }
}
