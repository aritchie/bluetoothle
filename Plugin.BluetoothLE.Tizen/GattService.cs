using System;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        public GattService(IDevice device, Guid uuid, bool primary) : base(device, uuid, primary)
        {
        }


        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            throw new NotImplementedException();
        }
    }
}
