using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;

namespace Acr.Ble
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly CBCharacteristic native;


        public GattCharacteristic(IGattService service, CBCharacteristic native) : base(service, native.UUID.ToGuid(), (CharacteristicProperties)(int)native.Properties)
        {
            this.native = native;
        }


        public override IObservable<object> Write(byte[] value)
        {
            this.AssertWrite();

            return Observable.Create<object>(ob =>
            {
                var data = NSData.FromArray(value);
                var p = this.native.Service.Peripheral;
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.UUID.Equals(this.native.UUID))
                    {
                        if (args.Error != null)
                            ob.OnError(new ArgumentException($"Error writing characteristic - {args.Error.LocalizedDescription}"));
                        else
                        {
                            this.Value = value;
                            this.WriteSubject.OnNext(this.Value);
                            ob.Respond(null);
                        }
                    }
                });

                if (this.Properties.HasFlag(CharacteristicProperties.WriteNoResponse))
                {
                    p.WriteValue(data, this.native, CBCharacteristicWriteType.WithoutResponse);
                    this.Value = value;
                    this.WriteSubject.OnNext(this.Value);
                    ob.Respond(null);
                }
                else
                {
                    p.WroteCharacteristicValue += handler;
                    p.WriteValue(data, this.native, CBCharacteristicWriteType.WithResponse);
                }
                return () => p.WroteCharacteristicValue -= handler;
            });
        }


        public override IObservable<byte[]> Read()
        {
            this.AssertRead();

            return Observable.Create<byte[]>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (args.Characteristic.UUID.Equals(this.native.UUID))
                    {
                        var value = this.native.Value.ToArray();
                        this.Value = value;
                        ob.Respond(this.Value);
                    }
                });
                this.native.Service.Peripheral.UpdatedCharacterteristicValue += handler;
                this.native.Service.Peripheral.ReadValue(this.native);

                return () => this.native.Service.Peripheral.UpdatedCharacterteristicValue -= handler;
            });
        }


        IObservable<byte[]> notifyOb;
        public override IObservable<byte[]> SubscribeToNotifications()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable
                .Create<byte[]>(ob =>
                {
                    var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                    {
                        if (args.Characteristic.UUID.Equals(this.native.UUID))
                        {
                            this.Value = this.native.Value.ToArray();
                            ob.OnNext(this.Value);
                            this.NotifySubject.OnNext(this.Value);
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
            this.descriptorOb = this.descriptorOb ?? Observable
                .Create<IGattDescriptor>(ob =>
                {
                    var p = this.native.Service.Peripheral;
                    var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                    {
                        if (this.native.Descriptors == null)
                            return;

                        foreach (var dnative in this.native.Descriptors)
                        {
                            var wrap = new GattDescriptor(this, dnative);
                            ob.OnNext(wrap);
                        }
                    });
                    p.DiscoveredDescriptor += handler;
                    p.DiscoverDescriptors(this.native);

                    return () => p.DiscoveredDescriptor -= handler;
                })
                .Distinct(x => x.Uuid)
                .Publish()
                .RefCount();

            return this.descriptorOb;
        }
    }
}