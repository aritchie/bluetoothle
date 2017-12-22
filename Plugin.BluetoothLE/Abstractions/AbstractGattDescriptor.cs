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
            this.WriteSubject = new Subject<DescriptorGattResult>();
            this.ReadSubject = new Subject<DescriptorGattResult>();
        }


        protected Subject<DescriptorGattResult> WriteSubject { get; }
        protected Subject<DescriptorGattResult> ReadSubject { get; }

        public IGattCharacteristic Characteristic { get; }
        public virtual string Description => Dictionaries.GetDescriptorDescription(this.Uuid);

        public Guid Uuid { get; }
        public byte[] Value { get; protected set; }
        public abstract IObservable<DescriptorGattResult> Write(byte[] data);
        public abstract IObservable<DescriptorGattResult> Read();

        public virtual IObservable<DescriptorGattResult> WhenRead() => this.ReadSubject;
        public virtual IObservable<DescriptorGattResult> WhenWritten() => this.WriteSubject;
    }
}