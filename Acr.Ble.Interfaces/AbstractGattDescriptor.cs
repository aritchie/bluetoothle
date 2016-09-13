using System;
using System.Reactive.Subjects;


namespace Acr.Ble
{
    public abstract class AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, Guid uuid)
        {
            this.Characteristic = characteristic;
            this.Uuid = uuid;
            this.WriteSubject = new Subject<byte[]>();
            this.ReadSubject = new Subject<byte[]>();
        }


        protected Subject<byte[]> WriteSubject;
        protected Subject<byte[]> ReadSubject;

        public IGattCharacteristic Characteristic { get; }
        public virtual string Description => Dictionaries.GetDescriptorDescription(this.Uuid);

        public Guid Uuid { get; }
        public byte[] Value { get; protected set; }
        public abstract IObservable<object> Write(byte[] data);
        public abstract IObservable<byte[]> Read();

        public virtual IObservable<byte[]> WhenRead() => this.ReadSubject;
        public virtual IObservable<byte[]> WhenWritten() => this.WriteSubject;
    }
}