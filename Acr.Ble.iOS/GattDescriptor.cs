using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Acr.Ble
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly CBDescriptor native;


        public GattDescriptor(IGattCharacteristic characteristic, CBDescriptor native) : base(characteristic, native.UUID.ToGuid())
        {
            this.native = native;
        }


        public override IObservable<byte[]> Read()
        {
            return Observable.Create<byte[]>(ob =>
            {
                var p = this.native.Characteristic.Service.Peripheral;

                var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = ((NSData) args.Descriptor.Value).ToArray();
                            ob.Respond(this.Value);
                            this.ReadSubject.OnNext(this.Value);
                        }
                    }
                });
                p.UpdatedValue += handler;
                p.ReadValue(this.native);
                return () => p.UpdatedValue -= handler;
            });
        }


        public override IObservable<object> Write(byte[] data)
        {
            return Observable.Create<object>(ob =>
            {
                var p = this.native.Characteristic.Service.Peripheral;

                var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = data;
                            ob.Respond(this.Value);
                            this.WriteSubject.OnNext(this.Value);
                        }
                    }
                });

                p.WroteDescriptorValue += handler;
                var nsdata = NSData.FromArray(data);
                p.WriteValue(nsdata, this.native);

                return () => p.WroteDescriptorValue -= handler;
            });
        }


        public override int GetHashCode()
        {
            return this.native.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattDescriptor;
            if (other == null)
                return false;

            if (!this.native.Equals(other.native))
                return false;

            return true;
        }


        public override string ToString()
        {
            return this.Uuid.ToString();
        }
    }
}