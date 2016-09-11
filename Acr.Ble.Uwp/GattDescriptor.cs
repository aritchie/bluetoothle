using System;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor;


namespace Acr.Ble
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        public GattDescriptor(IGattCharacteristic characteristic, Guid uuid) : base(characteristic, uuid)
        {
        }


        public override IObservable<object> Write(byte[] data)
        {
            throw new NotImplementedException();
        }


        public override IObservable<byte[]> Read()
        {
            throw new NotImplementedException();
        }
    }
}
