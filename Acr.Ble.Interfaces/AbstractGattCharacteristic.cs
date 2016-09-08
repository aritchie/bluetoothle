using System;


namespace Acr.Ble
{
    public abstract class AbstractGattCharacteristic : IGattCharacteristic
    {
        protected AbstractGattCharacteristic(IGattService service, Guid uuid, CharacteristicProperties properties)
        {
            this.Service = service;
            this.Uuid = uuid;
            this.Properties = properties;
        }


        public IGattService Service { get; }
        public virtual string Description => Dictionaries.GetCharacteristicDescription(this.Uuid.ToString());
        public bool IsNotifying { get; protected set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public byte[] Value { get; protected set; }

        public abstract IObservable<byte[]> Read();
        public abstract IObservable<object> Write(byte[] value);
        public abstract IObservable<byte[]> WhenNotificationOccurs();
        public abstract IObservable<IGattDescriptor> WhenDescriptorDiscovered();


        protected virtual void AssertWrite()
        {
            if (!this.CanWrite())
                throw new ArgumentException($"This characteristic '{this.Uuid}' does not support writes");        
        }


        protected virtual void AssertRead()
        {
            if (!this.CanRead())
                throw new ArgumentException($"This characteristic '{this.Uuid}' does not support reads");        
        }


        protected virtual void AssertNotify()
        {
            if (!this.CanNotify())
                throw new ArgumentException($"This characteristic '{this.Uuid}' does not support notifications");        
        }
    }
}
