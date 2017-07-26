using System;
using System.Reactive.Linq;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly GattService1 native;


        public GattService(GattService1 native, IDevice device) : base(device, Guid.Parse(native.UUID), native.Primary)
        {
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered() => Observable.Create<IGattCharacteristic>(ob =>
        {
            return () =>
            {

            };
        });
    }
}
