using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE.Server
{
    public class GattCharacteristic : AbstractGattCharacteristic, IIosGattCharacteristic
    {
        readonly CBPeripheralManager manager;
        readonly IDictionary<NSUuid, IDevice> subscribers;

        public CBMutableCharacteristic Native { get; }


        public GattCharacteristic(CBPeripheralManager manager,
                                  IGattService service,
                                  Guid characteristicUuid,
                                  CharacteristicProperties properties,
                                  GattPermissions permissions) : base(service, characteristicUuid, properties, permissions)
        {
            this.manager = manager;
            this.subscribers = new ConcurrentDictionary<NSUuid, IDevice>();

#if __TVOS__
#else
            this.Native = new CBMutableCharacteristic(
                characteristicUuid.ToCBUuid(),
                properties.ToNative(),
                null,
                (CBAttributePermissions) (int) permissions // TODO
            );
#endif
        }


        public override IReadOnlyList<IDevice> SubscribedDevices
        {
            get
            {
                lock (this.subscribers)
                {
                    return new ReadOnlyCollection<IDevice>(this.subscribers.Values.ToArray());
                }
            }
        }


        public override void Broadcast(byte[] value, params IDevice[] devices)
        {
            var data = NSData.FromArray(value);
            var devs = devices.OfType<Device>().ToList();
            if (devs.Count == 0)
            {
                devs = this.SubscribedDevices.OfType<Device>().ToList();
            }
            this.manager.UpdateValue(data, this.Native, devs.Select(x => x.Central).ToArray());
        }


        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IDevice[] devices)
        {
            return Observable.Create<CharacteristicBroadcast>(ob =>
            {
                var data = NSData.FromArray(value);

                var devs = devices.OfType<Device>().ToList();
                if (devs.Count == 0)
                {
                    devs = this.SubscribedDevices.OfType<Device>().ToList();
                }
                this.manager.UpdateValue(data, this.Native, devs.Select(x => x.Central).ToArray());

                var indicate = this.Properties.HasFlag(CharacteristicProperties.Indicate);
                foreach (var dev in devs)
                {
                    ob.OnNext(new CharacteristicBroadcast(dev, this, value, indicate, true));
                }
                ob.OnCompleted();

                return Disposable.Empty;
            });
        }


        IObservable<DeviceSubscriptionEvent> subOb;

        public override IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged()
        {
            this.subOb = this.subOb ?? Observable.Create<DeviceSubscriptionEvent>(ob =>
            {
                var sub = this.CreateSubHandler(ob, true);
                var unsub = this.CreateSubHandler(ob, false);

                this.manager.CharacteristicSubscribed += sub;
                this.manager.CharacteristicUnsubscribed += unsub;

                return () =>
                {
                    this.manager.CharacteristicSubscribed -= sub;
                    this.manager.CharacteristicUnsubscribed -= unsub;
                };
            })
            .Publish()
            .RefCount();

            return this.subOb;
        }


        IObservable<WriteRequest> writeOb;

        public override IObservable<WriteRequest> WhenWriteReceived()
        {
            this.writeOb = this.writeOb ?? Observable.Create<WriteRequest>(ob =>
            {
                var handler = new EventHandler<CBATTRequestsEventArgs>((sender, args) =>
                {
                    var writeWithResponse = this.Properties.HasFlag(CharacteristicProperties.Write);
                    foreach (var native in args.Requests)
                    {
                        if (native.Characteristic.Equals(this.Native))
                        {
                            var device = new Device(native.Central);
                            var request = new WriteRequest(device, native.Value.ToArray(), (int)native.Offset, false);
                            ob.OnNext(request);

                            if (writeWithResponse)
                            {
                                var status = (CBATTError) Enum.Parse(typeof(CBATTError), request.Status.ToString());
                                this.manager.RespondToRequest(native, status);
                            }
                        }
                    }
                });
                this.manager.WriteRequestsReceived += handler;
                return () => this.manager.WriteRequestsReceived -= handler;
            })
            .Publish()
            .RefCount();

            return this.writeOb;
        }


        IObservable<ReadRequest> readOb;

        public override IObservable<ReadRequest> WhenReadReceived()
        {
            this.readOb = this.readOb ?? Observable.Create<ReadRequest>(ob =>
            {
                var handler = new EventHandler<CBATTRequestEventArgs>((sender, args) =>
                {
                    if (args.Request.Characteristic.Equals(this.Native))
                    {
                        var device = new Device(args.Request.Central);
                        var request = new ReadRequest(device, (int)args.Request.Offset);
                        ob.OnNext(request);

                        var nativeStatus = (CBATTError) Enum.Parse(typeof(CBATTError), request.Status.ToString());
                        args.Request.Value = NSData.FromArray(request.Value);
                        this.manager.RespondToRequest(args.Request, nativeStatus);
                    }
                });
                this.manager.ReadRequestReceived += handler;
                return () => this.manager.ReadRequestReceived -= handler;
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }


        protected override IGattDescriptor CreateNative(Guid uuid, byte[] value)
        {
            return new GattDescriptor(this, uuid, value);
        }


        protected virtual EventHandler<CBPeripheralManagerSubscriptionEventArgs> CreateSubHandler(IObserver<DeviceSubscriptionEvent> ob, bool subscribing)
        {
            return (sender, args) =>
            {
                // on has a subcription or has none
                if (!args.Characteristic.Equals(this.Native))
                    return;

                if (subscribing)
                {
                    var device = this.GetOrAdd(args.Central);
                    ob.OnNext(new DeviceSubscriptionEvent(device, true));
                }
                else
                {
                    var device = this.Remove(args.Central);
                    if (device != null)
                        ob.OnNext(new DeviceSubscriptionEvent(device, false));
                }
            };
        }


        IDevice GetOrAdd(CBCentral central)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(central.Identifier))
                    return this.subscribers[central.Identifier];

                var device = new Device(central);
                this.subscribers.Add(central.Identifier, device);
                return device;
            }
        }


        IDevice Remove(CBCentral central)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(central.Identifier))
                {
                    var device = this.subscribers[central.Identifier];
                    this.subscribers.Remove(central.Identifier);
                    return device;
                }
                return null;
            }
        }
    }
}
