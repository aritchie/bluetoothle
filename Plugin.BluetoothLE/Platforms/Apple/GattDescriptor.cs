using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly GattCharacteristic characteristicObj;
        readonly CBDescriptor native;

        public CBCharacteristic NativeCharacteristic => this.characteristicObj.NativeCharacteristic;
        public CBService NativeService => this.characteristicObj.NativeService;
        public CBPeripheral Peripheral => this.characteristicObj.Peripheral;


        public GattDescriptor(GattCharacteristic characteristic, CBDescriptor native)
                       : base(characteristic, native.UUID.ToGuid())
        {
            this.characteristicObj = characteristic;
            this.native = native;
        }


        public override IObservable<DescriptorGattResult> Read()
        {
            return Observable.Create<DescriptorGattResult>(ob =>
            {
                var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
                {
                    if (!this.Equals(args.Descriptor))
                    {
                        if (args.Error != null)
                        {
                            ob.OnNext(this.ToResult(
                                GattEvent.ReadError,
                                args.Error.ToString()
                            ));
                        }
                        else
                        {
                            this.Value = ((NSData) args.Descriptor.Value).ToArray();

                            var result = this.ToResult(GattEvent.Read, this.Value);
                            this.ReadSubject.OnNext(result);
                            ob.Respond(result);
                        }
                    }
                });
                this.Peripheral.UpdatedValue += handler;
                this.Peripheral.ReadValue(this.native);

                return () => this.Peripheral.UpdatedValue -= handler;
            });
        }


        public override IObservable<DescriptorGattResult> Write(byte[] data)
        {
            return Observable.Create<DescriptorGattResult>(ob =>
            {
                var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
                {
                    if (!this.Equals(args.Descriptor))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = data;

                            var result = this.ToResult(GattEvent.Write, this.Value);
                            this.WriteSubject.OnNext(result);
                            ob.Respond(result);
                        }
                    }
                });

                var nsdata = NSData.FromArray(data);
                this.Peripheral.WroteDescriptorValue += handler;
                this.Peripheral.WriteValue(nsdata, this.native);

                return () => this.Peripheral.WroteDescriptorValue -= handler;
            });
        }


        bool Equals(CBDescriptor descriptor)
        {
            if (!this.native.UUID.Equals(descriptor.UUID))
                return false;

            if (!this.NativeCharacteristic.UUID.Equals(descriptor.Characteristic.UUID))
                return false;

            if (!this.NativeService.UUID.Equals(descriptor.Characteristic.Service.UUID))
                return false;

			if (!this.Peripheral.Identifier.Equals(descriptor.Characteristic.Service.Peripheral.Identifier))
                return false;

            return true;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattDescriptor;
            if (other == null)
                return false;

			if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}