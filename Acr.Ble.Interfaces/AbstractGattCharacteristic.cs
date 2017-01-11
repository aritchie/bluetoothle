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


        protected Subject<CharacteristicResult> NotifySubject { get; } = new Subject<CharacteristicResult>();
        protected Subject<CharacteristicResult> ReadSubject { get; } = new Subject<CharacteristicResult>();
        protected Subject<CharacteristicResult> WriteSubject { get; } = new Subject<CharacteristicResult>();

        public IGattService Service { get; }
        public virtual string Description => Dictionaries.GetCharacteristicDescription(this.Uuid);
        public bool IsNotifying { get; protected set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public byte[] Value { get; protected set; }

        public abstract IObservable<IGattDescriptor> WhenDescriptorDiscovered();
        public abstract IObservable<CharacteristicResult> SubscribeToNotifications();
        public virtual IObservable<CharacteristicResult> WhenNotificationReceived() => this.NotifySubject;

        public abstract IObservable<CharacteristicResult> Read();
        public virtual IObservable<CharacteristicResult> WhenRead() => this.ReadSubject;

        public virtual IObservable<CharacteristicResult> WhenWritten() => this.WriteSubject;
        public abstract void WriteWithoutResponse(byte[] value, bool reliableWrite);
        public abstract IObservable<CharacteristicResult> Write(byte[] value, bool reliableWrite);


        public virtual IObservable<BleWriteSegment> BlobWrite(byte[] value, bool reliableWrite)
        {
            // don't need to dispose of memorystream
            return this.BlobWrite(new MemoryStream(value), reliableWrite);
        }


        public virtual IObservable<BleWriteSegment> BlobWrite(Stream stream, bool reliableWrite)
        {
            return Observable.Create<BleWriteSegment>(async ob =>
            {
                var mtu = this.Service.Device.GetCurrentMtuSize();
                var cts = new CancellationTokenSource();
                var buffer = new byte[mtu];
                var read = stream.Read(buffer, 0, buffer.Length);
                var pos = read;
                var len = Convert.ToInt32(stream.Length);

                while (!cts.IsCancellationRequested && read > 0)
                {
                    await this.Write(buffer, reliableWrite).RunAsync(cts.Token);
                    var seg = new BleWriteSegment(buffer, pos, len);
                    ob.OnNext(seg);

                    read = stream.Read(buffer, 0, buffer.Length);
                    pos += read;
                }
                ob.OnCompleted();

                return () => cts.Cancel();
            });
        }
    }
}
