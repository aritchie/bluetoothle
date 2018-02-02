using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Acr;
using Android.Bluetooth;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly BluetoothGattDescriptor native;
        readonly DeviceContext context;


        public GattDescriptor(IGattCharacteristic characteristic,
                              DeviceContext context,
                              BluetoothGattDescriptor native) : base(characteristic, native.Uuid.ToGuid())
        {
            this.context = context;
            this.native = native;
        }


        public override byte[] Value => this.native.GetValue();

        public override IObservable<DescriptorGattResult> Write(byte[] data) => this.context.Lock(Observable.Create<DescriptorGattResult>(async ob =>
        {
            var sub = this.context
                .Callbacks
                .DescriptorWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    var result = args.IsSuccessful
                        ? this.ToResult(GattEvent.Write, data)
                        : this.ToResult(GattEvent.WriteError, $"Failed to write descriptor value - {args.Status}");

                    ob.Respond(result);
                });

            await this.context.Marshall(() =>
            {
                if (!this.native.SetValue(data))
                    ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to set descriptor value"));

                else if (!this.context.Gatt.WriteDescriptor(this.native))
                    ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to write to descriptor"));
            })
            .ToTask()
            .ConfigureAwait(false);

            return sub;
        }));


        public override IObservable<DescriptorGattResult> Read() => this.context.Lock(Observable.Create<DescriptorGattResult>(async ob =>
        {
            var sub = this.context
                .Callbacks
                .DescriptorRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    var result = args.IsSuccessful
                        ? this.ToResult(GattEvent.Write, this.Value)
                        : this.ToResult(GattEvent.ReadError, $"Failed to read descriptor value - {args.Status}");

                    ob.Respond(result);
                });

            await this.context.Marshall(() =>
            {
                if (!this.context.Gatt.ReadDescriptor(this.native))
                    ob.Respond(this.ToResult(GattEvent.ReadError, "Failed to read descriptor"));
            })
            .ToTask()
            .ConfigureAwait(false);

            return sub;
        }));


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
        public override string ToString() => $"Descriptor: {this.Uuid}";


        bool NativeEquals(GattDescriptorEventArgs args)
        {
            if (this.context.Gatt == null || args.Descriptor?.Characteristic?.Service == null)
                return false;

            if (this.native.Equals(args.Descriptor))
                return true;

            if (!this.native.Uuid.Equals(args.Descriptor.Uuid))
                return false;

            if (!this.native.Characteristic.Uuid.Equals(args.Descriptor.Characteristic.Uuid))
                return false;

            if (!this.native.Characteristic.Service.Uuid.Equals(args.Descriptor.Characteristic.Service.Uuid))
                return false;

            if (!this.context.Gatt.Equals(args.Gatt))
                return false;

            return true;
        }
    }
}