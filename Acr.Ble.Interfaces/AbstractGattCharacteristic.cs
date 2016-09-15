using System;
using System.Reactive.Subjects;


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


        protected Subject<byte[]> NotifySubject { get; } = new Subject<byte[]>();
        protected Subject<byte[]> ReadSubject { get; } = new Subject<byte[]>();
        protected Subject<byte[]> WriteSubject { get; } = new Subject<byte[]>();

        public IGattService Service { get; }
        public virtual string Description => Dictionaries.GetCharacteristicDescription(this.Uuid);
        public bool IsNotifying { get; protected set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public byte[] Value { get; protected set; }

        public abstract IObservable<IGattDescriptor> WhenDescriptorDiscovered();
        public abstract IObservable<byte[]> SubscribeToNotifications();
        public virtual IObservable<byte[]> WhenNotificationReceived() => this.NotifySubject;
        public abstract IObservable<byte[]> Read();
        public virtual IObservable<byte[]> WhenRead() => this.ReadSubject;
        public abstract IObservable<object> Write(byte[] value);
        public virtual IObservable<byte[]> WhenWritten() => this.WriteSubject;


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
