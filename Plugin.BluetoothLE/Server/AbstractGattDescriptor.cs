using System;


namespace Plugin.BluetoothLE.Server
{
    public abstract class  AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, Guid descriptorUuid, byte[] value)
        {
            this.Characteristic = characteristic;
            this.Uuid = descriptorUuid;
            this.Value = value;
        }


        public IGattCharacteristic Characteristic { get; }
        public Guid Uuid { get; }
        public byte[] Value { get; }
    }
}
