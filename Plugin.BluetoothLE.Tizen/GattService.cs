using System;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        public GattService(BluetoothGattService native, IDevice device, Guid uuid, bool primary) : base(device, uuid, primary)
        {
        }


        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            throw new NotImplementedException();
        }
    }
}
