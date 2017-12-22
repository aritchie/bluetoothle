using System;
using System.Reactive.Linq;
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


        public override IObservable<DescriptorGattResult> Write(byte[] data) => this.context.Lock(Observable.Create<DescriptorGattResult>(async ob =>
        {
            var sub = this.context
                .Callbacks
                .DescriptorWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (!args.IsSuccessful)
                    {
                        ob.OnNext(this.ToResult(GattEvent.WriteError, $"Failed to write descriptor value - {args.Status}"));
                    }
                    else
                    {
                        this.Value = data;

                        var result = this.ToResult(GattEvent.Write, data);
                        ob.Respond(result);
                        this.WriteSubject.OnNext(result);
                    }
                });

            await this.context.Marshall(() =>
            {
                this.native.SetValue(data);
                var result = this.context.Gatt.WriteDescriptor(this.native);
                if (!result)
                    ob.Respond(this.ToResult(GattEvent.WriteError, "Failed to write to descriptor"));
            });

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
                    if (!args.IsSuccessful)
                    {
                        ob.OnNext(this.ToResult(GattEvent.ReadError, $"Failed to read descriptor value - {args.Status}"));
                    }
                    else
                    {
                        this.Value = this.native.GetValue();

                        var result = this.ToResult(GattEvent.Write, this.Value);
                        ob.Respond(result);
                        this.ReadSubject.OnNext(result);
                    }
                });

            await this.context.Marshall(() =>
            {
                var result = this.context.Gatt.ReadDescriptor(this.native);
                if (!result)
                    ob.Respond(this.ToResult(GattEvent.ReadError, "Failed to read descriptor"));
            });

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