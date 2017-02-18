using System;
using System.Reactive.Linq;
using Android.Bluetooth;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
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


        public override IObservable<DescriptorResult> Write(byte[] data)
        {
            return Observable.Create<DescriptorResult>(ob =>
            {
                var handler = new EventHandler<GattDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.Equals(this.native))
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnError(new ArgumentException($"Failed to write descriptor value - {this.Uuid} - {args.Status}"));
                        }
                        else
                        {
                            this.Value = data;

                            var result = new DescriptorResult(this, DescriptorEvent.Write, data);
                            ob.Respond(result);
                            this.WriteSubject.OnNext(result);
                        }
                    }
                });
                this.context.Callbacks.DescriptorWrite += handler;
                AndroidConfig.SyncPost(() =>
                {
                    this.native.SetValue(data);
                    this.context.Gatt.WriteDescriptor(this.native);
                });
                return () => this.context.Callbacks.DescriptorWrite -= handler;
            });
        }


        public override IObservable<DescriptorResult> Read()
        {
            //this.native.Permissions == GattDescriptorPermission.Read
            return Observable.Create<DescriptorResult>(ob =>
            {
                var handler = new EventHandler<GattDescriptorEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.Equals(this.native))
                    {
                        if (!args.IsSuccessful)
                        {
                            ob.OnError(new ArgumentException($"Failed to read descriptor value {this.Uuid} - {args.Status}"));
                        }
                        else
                        {
                            this.Value = this.native.GetValue();

                            var result = new DescriptorResult(this, DescriptorEvent.Write, this.Value);
                            ob.Respond(result);
                            this.ReadSubject.OnNext(result);
                        }
                    }
                });
                this.context.Callbacks.DescriptorRead += handler;
                this.context.Gatt.ReadDescriptor(this.native);
                return () => this.context.Callbacks.DescriptorRead -= handler;
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

