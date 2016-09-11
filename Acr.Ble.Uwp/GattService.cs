using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;


namespace Acr.Ble
{
    public class GattService : AbstractGattService
    {
        readonly Native native;


        public GattService(Native native, IDevice device) : base(device, native.Uuid, false)
        {
            this.native = native;
        }


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = this.native.GetAllCharacteristics();
                foreach (var characteristic in characteristics)
                {
                    var wrap = new GattCharacteristic(characteristic, this);
                    ob.OnNext(wrap);
                }
                return Disposable.Empty;
            })
            .Replay();

            return this.characteristicOb;
        }
    }
}
