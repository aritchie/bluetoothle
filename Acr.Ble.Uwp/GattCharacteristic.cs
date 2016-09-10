using System;


namespace Acr.Ble
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        public GattCharacteristic(IGattService service, Guid uuid, CharacteristicProperties properties) : base(service, uuid, properties)
        {
        }


        public override IObservable<byte[]> Read()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            throw new NotImplementedException();
        }


        public override IObservable<object> Write(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<byte[]> WhenNotificationOccurs()
        {
            throw new NotImplementedException();
        }
    }
}
