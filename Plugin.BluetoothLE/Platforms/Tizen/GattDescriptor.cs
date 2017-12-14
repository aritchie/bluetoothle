using System;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        public GattDescriptor(IGattCharacteristic characteristic, Guid uuid) : base(characteristic, uuid)
        {
        }


        public override IObservable<DescriptorResult> Write(byte[] data)
        {
            throw new NotImplementedException();
        }


        public override IObservable<DescriptorResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}
