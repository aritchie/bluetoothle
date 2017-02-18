using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly CBDescriptor native;


        public GattDescriptor(IGattCharacteristic characteristic, CBDescriptor native) : base(characteristic, native.UUID.ToGuid())
        {
            this.native = native;
        }


        public override IObservable<DescriptorResult> Read()
        {
            return Observable.Create<DescriptorResult>(ob =>
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

                            var result = new DescriptorResult(this, DescriptorEvent.Read, this.Value);
                            ob.Respond(result);
                            this.ReadSubject.OnNext(result);
                        }
                    }
                });
                p.UpdatedValue += handler;
                p.ReadValue(this.native);
                return () => p.UpdatedValue -= handler;
            });
        }


        public override IObservable<DescriptorResult> Write(byte[] data)
        {
            return Observable.Create<DescriptorResult>(ob =>
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

                            var result = new DescriptorResult(this, DescriptorEvent.Write, this.Value);
                            ob.Respond(result);
                            this.WriteSubject.OnNext(result);
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