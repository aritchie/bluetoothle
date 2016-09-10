using System;


namespace Acr.Ble
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
