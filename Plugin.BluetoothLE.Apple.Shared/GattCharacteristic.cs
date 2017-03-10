using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly CBCharacteristic native;


        public GattCharacteristic(IGattService service, CBCharacteristic native)
                : base(service, native.UUID.ToGuid(), (CharacteristicProperties)(int)native.Properties)
        {
            this.native = native;
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            this.InternalWriteNoResponse(null, value);
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            this.AssertWrite(false);

            return Observable.Create<CharacteristicResult>(ob =>
            {
                var data = NSData.FromArray(value);
                var p = this.native.Service.Peripheral;
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = value;

                            var result = new CharacteristicResult(this, CharacteristicEvent.Write, value);
                            ob.Respond(result);
                            this.WriteSubject.OnNext(result);
                        }
                    }
                });

                if (this.Properties.HasFlag(CharacteristicProperties.Write))
                {
                    p.WroteCharacteristicValue += handler;
                    p.WriteValue(data, this.native, CBCharacteristicWriteType.WithResponse);
                }
                else
                {
                    this.InternalWriteNoResponse(ob, value);
                }
                return () => p.WroteCharacteristicValue -= handler;
            });
        }


        public override IObservable<CharacteristicResult> Read()
        {
            this.AssertRead();

            return Observable.Create<CharacteristicResult>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = this.native.Value.ToArray();
                            var result = new CharacteristicResult(this, CharacteristicEvent.Read, this.Value);
                            ob.Respond(result);
                            this.ReadSubject.OnNext(result);
                        }
                    }
                });
                this.native.Service.Peripheral.UpdatedCharacterteristicValue += handler;
                this.native.Service.Peripheral.ReadValue(this.native);

                return () => this.native.Service.Peripheral.UpdatedCharacterteristicValue -= handler;
            });
        }


        IObservable<CharacteristicResult> notifyOb;
        public override IObservable<CharacteristicResult> SubscribeToNotifications()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<CharacteristicResult>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                        {
                            ob.OnError(new ArgumentException(args.Error.ToString()));
                        }
                        else
                        {
                            this.Value = this.native.Value.ToArray();

                            var result = new CharacteristicResult(this, CharacteristicEvent.Notification, this.Value);
                            ob.OnNext(result);
                            this.NotifySubject.OnNext(result);
                        }
                    }
                });
                this.native.Service.Peripheral.UpdatedCharacterteristicValue += handler;
                this.native.Service.Peripheral.SetNotifyValue(true, this.native);

                return () =>
                {
                    this.native.Service.Peripheral.SetNotifyValue(false, this.native);
                    this.native.Service.Peripheral.UpdatedCharacterteristicValue -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.notifyOb;
        }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(ob =>
            {
                var descriptors = new Dictionary<Guid, IGattDescriptor>();

                var p = this.native.Service.Peripheral;
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.native.Descriptors == null)
                        return;

                    foreach (var dnative in this.native.Descriptors)
                    {
                        var wrap = new GattDescriptor(this, dnative);
                        if (!descriptors.ContainsKey(wrap.Uuid))
                        {
                            descriptors.Add(wrap.Uuid, wrap);
                            ob.OnNext(wrap);
                        }
                    }
                });
                p.DiscoveredDescriptor += handler;
                p.DiscoverDescriptors(this.native);

                return () => p.DiscoveredDescriptor -= handler;
            })
            .Replay()
            .RefCount();

            return this.descriptorOb;
        }


        void InternalWriteNoResponse(IObserver<CharacteristicResult> ob, byte[] value)
        {
            var data = NSData.FromArray(value);
            var p = this.native.Service.Peripheral;
            p.WriteValue(data, this.native, CBCharacteristicWriteType.WithoutResponse);
            this.Value = value;
            var result = new CharacteristicResult(this, CharacteristicEvent.Write, value);
            this.WriteSubject.OnNext(result);
            ob?.Respond(result);
        }


        public override int GetHashCode()
        {
            return this.native.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
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