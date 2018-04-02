using System;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        public GattDescriptor(IGattCharacteristic characteristic, Guid uuid) : base(characteristic, uuid)
        {
        }


        public override byte[] Value { get; }


        public override IObservable<DescriptorGattResult> Write(byte[] data)
        {
            throw new NotImplementedException();
        }


        public override IObservable<DescriptorGattResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}
