using System;
using System.Reactive.Subjects;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, Guid uuid)
        {
            this.Characteristic = characteristic;
            this.Uuid = uuid;
            this.WriteSubject = new Subject<DescriptorResult>();
            this.ReadSubject = new Subject<DescriptorResult>();
        }


        protected Subject<DescriptorResult> WriteSubject { get; }
        protected Subject<DescriptorResult> ReadSubject { get; }

        public IGattCharacteristic Characteristic { get; }
        public virtual string Description => Dictionaries.GetDescriptorDescription(this.Uuid);

        public Guid Uuid { get; }
        public byte[] Value { get; protected set; }
        public abstract IObservable<DescriptorResult> Write(byte[] data);
        public abstract IObservable<DescriptorResult> Read();

        public virtual IObservable<DescriptorResult> WhenRead() => this.ReadSubject;
        public virtual IObservable<DescriptorResult> WhenWritten() => this.WriteSubject;
    }
}