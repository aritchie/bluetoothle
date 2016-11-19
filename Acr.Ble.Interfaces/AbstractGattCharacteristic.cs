using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

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

        public abstract void WriteWithoutResponse(byte[] value);
        public abstract IObservable<IGattDescriptor> WhenDescriptorDiscovered();
        public abstract IObservable<byte[]> SubscribeToNotifications();
        public virtual IObservable<byte[]> WhenNotificationReceived() => this.NotifySubject;

        public abstract IObservable<byte[]> Read();
        public virtual IObservable<byte[]> WhenRead() => this.ReadSubject;

        public abstract IObservable<object> Write(byte[] value);
        public virtual IObservable<byte[]> WhenWritten() => this.WriteSubject;


        public virtual IObservable<ArraySegment<byte>> BlobWrite(byte[] value)
        {
            // don't need to dispose of memorystream
            return this.BlobWrite(new MemoryStream(value));
        }


        public virtual IObservable<ArraySegment<byte>> BlobWrite(Stream stream)
        {
            
            return Observable.Create<ArraySegment<byte>>(async ob =>
            {
                // TODO: could request MTU increase on droid
                // TODO: should check MTU size for buffer size in any case
                var cts = new CancellationTokenSource();
                var buffer = new byte[20];
                var read = stream.Read(buffer, 0, buffer.Length);

                while (!cts.IsCancellationRequested && read > 0)
                {
                    await this.Write(buffer).RunAsync(cts.Token);
                    ob.OnNext(new ArraySegment<byte>(buffer, Convert.ToInt32(stream.Position), buffer.Length));
                    read = stream.Read(buffer, 0, buffer.Length);
                }
                ob.OnCompleted();

                return () => cts.Cancel();
            });            
        }
    }
}
