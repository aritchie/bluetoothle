using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly GattService serivceObj;
        public CBPeripheral Peripheral => this.serivceObj.Peripherial;
        public CBService NativeService => this.serivceObj.Service;
        public CBCharacteristic NativeCharacteristic { get; }


        public GattCharacteristic(GattService service, CBCharacteristic native) : base(service, native.UUID.ToGuid(), (CharacteristicProperties)(int)native.Properties)
        {
            this.serivceObj = service;
            this.NativeCharacteristic = native;
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            this.InternalWriteNoResponse(null, value);
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value)
        {
            this.AssertWrite(false);

            return Observable.Create<CharacteristicGattResult>(ob =>
            {
                var data = NSData.FromArray(value);
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.Equals(args.Characteristic))
                    {
                        if (args.Error != null)
                        {
                            ob.Respond(this.ToResult(
                                GattEvent.WriteError,
                                args.Error.ToString()
                            ));
                        }
                        else
                        {
                            this.Value = value;

                            var result = this.ToResult(GattEvent.Write, value);
                            this.WriteSubject.OnNext(result);
                            ob.Respond(result);
                        }
                    }
                });

                if (this.Properties.HasFlag(CharacteristicProperties.Write))
                {
                    this.Peripheral.WroteCharacteristicValue += handler;
                    this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithResponse);
                }
                else
                {
                    this.InternalWriteNoResponse(ob, value);
                }
                return () => this.Peripheral.WroteCharacteristicValue -= handler;
            });
        }


        public override IObservable<CharacteristicGattResult> Read()
        {
            this.AssertRead();

            return Observable.Create<CharacteristicGattResult>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.Equals(args.Characteristic))
                    {
                        if (args.Error != null)
                        {
                            ob.Respond(this.ToResult(
                                GattEvent.ReadError,
                                args.Error.ToString()
                            ));
                        }
                        else
                        {
                            this.Value = this.NativeCharacteristic.Value?.ToArray();
                            var result = this.ToResult(GattEvent.Read, this.Value);
                            this.ReadSubject.OnNext(result);
                            ob.Respond(result);
                        }
                    }
                });
                this.Peripheral.UpdatedCharacterteristicValue += handler;
                this.Peripheral.ReadValue(this.NativeCharacteristic);

                return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
            });
        }


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            this.AssertNotify();
            this.Peripheral.SetNotifyValue(true, this.NativeCharacteristic);
            // TODO
            //return Observable.Return(Unit.Default);
            return null;
        }


        public override IObservable<CharacteristicGattResult> DisableNotifications()
        {
            this.AssertNotify();
            this.Peripheral.SetNotifyValue(false, this.NativeCharacteristic);
            // TODO
            //return Observable.Return(Unit.Default);
            return null;
        }


        IObservable<CharacteristicGattResult> notifyOb;
        public override IObservable<CharacteristicGattResult> WhenNotificationReceived()
        {
            this.AssertNotify();

            this.notifyOb = this.notifyOb ?? Observable.Create<CharacteristicGattResult>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.Equals(args.Characteristic))
                    {
                        if (args.Error != null)
                        {
                            ob.OnNext(this.ToResult(
                                GattEvent.NotificationError,
                                args.Error.ToString()
                            ));
                            ob.OnError(new ArgumentException());
                        }
                        else
                        {
                            this.Value = this.NativeCharacteristic.Value?.ToArray();
                            var result = this.ToResult(GattEvent.Notification, this.Value);
                            ob.OnNext(result);
                        }
                    }
                });
                this.Peripheral.UpdatedCharacterteristicValue += handler;
                return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
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

                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.NativeCharacteristic.Descriptors == null)
                        return;

                    foreach (var dnative in this.NativeCharacteristic.Descriptors)
                    {
                        var wrap = new GattDescriptor(this, dnative);
                        if (!descriptors.ContainsKey(wrap.Uuid))
                        {
                            descriptors.Add(wrap.Uuid, wrap);
                            ob.OnNext(wrap);
                        }
                    }
                });
                this.Peripheral.DiscoveredDescriptor += handler;
                this.Peripheral.DiscoverDescriptors(this.NativeCharacteristic);

                return () => this.Peripheral.DiscoveredDescriptor -= handler;
            })
            .Replay()
            .RefCount();

            return this.descriptorOb;
        }


        void InternalWriteNoResponse(IObserver<CharacteristicGattResult> ob, byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
            this.Value = value;

            var result = this.ToResult(GattEvent.Write, value);
            this.WriteSubject.OnNext(result);
            ob?.Respond(result);
        }


        bool Equals(CBCharacteristic ch)
        {
            if (!this.NativeCharacteristic.UUID.Equals(ch.UUID))
                return false;

            if (!this.NativeService.UUID.Equals(ch.Service.UUID))
                return false;

			if (!this.Peripheral.Identifier.Equals(ch.Service.Peripheral.Identifier))
                return false;

            return true;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
            if (other == null)
                return false;

			if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.NativeCharacteristic.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}