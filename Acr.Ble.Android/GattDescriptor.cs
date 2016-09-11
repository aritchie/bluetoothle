using System;
using System.Reactive.Linq;
using Acr.Ble.Internals;
using Android.Bluetooth;


namespace Acr.Ble
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly BluetoothGattDescriptor native;
        readonly GattContext context;


        public GattDescriptor(IGattCharacteristic characteristic, GattContext context, BluetoothGattDescriptor native) : base(characteristic, native.Uuid.ToGuid())
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<object> Write(byte[] data)
        {
            return Observable.Create<object>(ob =>
            {
                var handler = new EventHandler<GattDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.Equals(this.native))
                    {
                        if (!args.IsSuccessful)
                            ob.OnError(new ArgumentException($"Failed to write descriptor value - {this.Uuid}"));
                        else
                        {
                            this.Value = data;
                            ob.OnNext(new object());
                            ob.OnCompleted();
                        }
                    }
                });
                this.context.Callbacks.DescriptorWrite += handler;
                this.native.SetValue(data);
                this.context.Gatt.WriteDescriptor(this.native);
                return () => this.context.Callbacks.DescriptorWrite -= handler;
            });
        }


        public override IObservable<byte[]> Read()
        {
            //this.native.Permissions == GattDescriptorPermission.Read
            return Observable.Create<byte[]>(ob =>
            {
                var handler = new EventHandler<GattDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.Equals(this.native))
                    {
                        if (!args.IsSuccessful)
                            ob.OnError(new ArgumentException($"Failed to read descriptor value - {this.Uuid}"));
                        else
                        {
                            this.Value = this.native.GetValue();
                            ob.OnNext(this.Value);
                            ob.OnCompleted();
                        }
                    }
                });
                this.context.Callbacks.DescriptorRead += handler;
                this.context.Gatt.ReadDescriptor(this.native);
                return () => this.context.Callbacks.DescriptorRead -= handler;
            });
        }
    }
}

