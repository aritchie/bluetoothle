using System;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly GattDescriptor1 native;


        public GattDescriptor(GattDescriptor1 native, IGattCharacteristic characteristic) : base(characteristic, Guid.Parse(native.UUID))
        {
            this.native = native;
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
