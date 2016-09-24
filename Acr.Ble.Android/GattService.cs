using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Acr.Ble.Internals;
using Android.Bluetooth;


namespace Acr.Ble
{
    public class GattService : AbstractGattService
    {
        readonly GattContext context;
        readonly BluetoothGattService native;


        public GattService(IDevice device, GattContext context, BluetoothGattService native) 
                : base(device, native.Uuid.ToGuid(), native.Type == GattServiceType.Primary)
        {
            this.context = context;
            this.native = native;
        }


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(ob =>
            {
                foreach (var nch in native.Characteristics) 
                {
                    var wrap = new GattCharacteristic(this, this.context, nch);
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