using System;
using System.Reactive.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly BluetoothGattService native;


        public GattService(BluetoothGattService native, IDevice device, Guid uuid, bool primary) : base(device, uuid, primary)
        {
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds) =>
            Observable.Create<IGattCharacteristic>(ob =>
            {
                return () => { };
            });


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics() =>
            Observable.Create<IGattCharacteristic>(ob =>
            {
                foreach (var ch in this.native.GetCharacteristics())
                {

                }

                return () => { };
            });
    }
}
