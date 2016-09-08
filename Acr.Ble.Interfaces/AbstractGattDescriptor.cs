using System;


namespace Acr.Ble
{
    public abstract class AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, Guid uuid)
        {
            this.Characteristic = characteristic;
            this.Uuid = uuid;
        }


        public IGattCharacteristic Characteristic { get; }
        public virtual string Description => Dictionaries.GetDescriptorDescription(this.Uuid.ToString());

        public Guid Uuid { get; }
        public byte[] Value { get; protected set; }
        public abstract IObservable<object> Write(byte[] data);
        public abstract IObservable<byte[]> Read();
    }
}