using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Android.Bluetooth;
using Java.Util;
using Plugin.BluetoothLE.Server.Internals;
using Observable = System.Reactive.Linq.Observable;


namespace Plugin.BluetoothLE.Server
{
    public class GattCharacteristic : AbstractGattCharacteristic, IDroidGattCharacteristic
    {
        public static readonly Guid NotifyDescriptorId = new Guid("00002902-0000-1000-8000-00805f9b34fb");
        public static readonly UUID NotifyDescriptorUuid = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        public static readonly byte[] NotifyEnabledBytes = BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        public static readonly byte[] NotifyDisableBytes = BluetoothGattDescriptor.DisableNotificationValue.ToArray();
        public static readonly byte[] IndicateEnableBytes = BluetoothGattDescriptor.EnableIndicationValue.ToArray();

        public BluetoothGattCharacteristic Native { get; }
        public BluetoothGattDescriptor NotificationDescriptor { get; }
        readonly GattContext context;
        readonly IDictionary<string, IDevice> subscribers;


        public GattCharacteristic(GattContext context,
                                  IGattService service,
                                  Guid uuid,
                                  CharacteristicProperties properties,
                                  GattPermissions permissions) : base(service, uuid, properties, permissions)
        {
            this.context = context;
            this.subscribers = new Dictionary<string, IDevice>();
            this.Native = new BluetoothGattCharacteristic(
                uuid.ToUuid(),
                properties.ToNative(),
                permissions.ToNative()
            );

            this.NotificationDescriptor = new BluetoothGattDescriptor(
                NotifyDescriptorId.ToUuid(),
                GattDescriptorPermission.Write | GattDescriptorPermission.Read
            );
            this.Native.AddDescriptor(this.NotificationDescriptor);
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
            this.Native.SetValue(value);

            if (devices == null || devices.Length == 0)
                devices = this.subscribers.Values.ToArray();

            foreach (var x in devices.OfType<Device>())
            {
                lock (this.context.ServerReadWriteLock)
                {
                    this.context.Server.NotifyCharacteristicChanged(x.Native, this.Native, false);
                }
            }
        }


        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IDevice[] devices)
        {
            return Observable.Create<CharacteristicBroadcast>(ob =>
            {
                var cancel = false;
                this.Native.SetValue(value);

                if (devices == null || devices.Length == 0)
                    devices = this.subscribers.Values.ToArray();

                var indicate = this.Properties.HasFlag(CharacteristicProperties.Indicate);
                foreach (var x in devices.OfType<Device>())
                {
                    if (!cancel)
                    {
                        lock (this.context.ServerReadWriteLock)
                        {
                            if (!cancel)
                            {
                                var result = this.context.Server.NotifyCharacteristicChanged(x.Native, this.Native, indicate);
                                ob.OnNext(new CharacteristicBroadcast(x, this, value, indicate, result));
                            }
                        }
                    }
                }

                ob.OnCompleted();
                return () => cancel = true;
            });
        }


        IObservable<DeviceSubscriptionEvent> subscriptionOb;
        public override IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged()
        {
            this.subscriptionOb = this.subscriptionOb ?? Observable.Create<DeviceSubscriptionEvent>(ob =>
            {
                var handler = new EventHandler<DescriptorWriteEventArgs>((sender, args) =>
                {
                    if (args.Descriptor.Equals(this.NotificationDescriptor))
                    {
                        if (args.Value.SequenceEqual(NotifyEnabledBytes) || args.Value.SequenceEqual(IndicateEnableBytes))
                        {
                            var device = this.GetOrAdd(args.Device);
                            ob.OnNext(new DeviceSubscriptionEvent(device, true));
                        }
                        else
                        {
                            var device = this.Remove(args.Device);
                            if (device != null)
                                ob.OnNext(new DeviceSubscriptionEvent(device, false));
                        }
                    }
                });
                var dhandler = new EventHandler<ConnectionStateChangeEventArgs>((sender, args) =>
                {
                    if (args.NewState != ProfileState.Disconnected)
                        return;

                    var device = this.Remove(args.Device);
                    if (device != null)
                        ob.OnNext(new DeviceSubscriptionEvent(device, false));
                });

                this.context.Callbacks.ConnectionStateChanged += dhandler;
                this.context.Callbacks.DescriptorWrite += handler;

                return () =>
                {
                    this.context.Callbacks.DescriptorWrite -= handler;
                    this.context.Callbacks.ConnectionStateChanged -= dhandler;
                };
            })
            .Publish()
            .RefCount();

            return this.subscriptionOb;
        }


        public override IObservable<WriteRequest> WhenWriteReceived()
        {
            return Observable.Create<WriteRequest>(ob =>
            {
                var handler = new EventHandler<CharacteristicWriteEventArgs>((sender, args) =>
                {
                    if (!args.Characteristic.Equals(this.Native))
                        return;

                    var device = new Device(args.Device);
                    var request = new WriteRequest(device, args.Value, args.Offset, args.ResponseNeeded);
                    ob.OnNext(request);

                    if (request.IsReplyNeeded)
                    {
                        lock (this.context.ServerReadWriteLock)
                        {
                            this.context.Server.SendResponse
                            (
                                args.Device,
                                args.RequestId,
                                request.Status.ToNative(),
                                request.Offset,
                                request.Value
                            );
                        }
                    }
                });
                this.context.Callbacks.CharacteristicWrite += handler;

                return () => this.context.Callbacks.CharacteristicWrite -= handler;
            });
        }


        public override IObservable<ReadRequest> WhenReadReceived()
        {
            return Observable.Create<ReadRequest>(ob =>
            {
                var handler = new EventHandler<CharacteristicReadEventArgs>((sender, args) =>
                {
                    if (!args.Characteristic.Equals(this.Native))
                        return;

                    var device = new Device(args.Device);
                    var request = new ReadRequest(device, args.Offset);
                    ob.OnNext(request);

                    lock (this.context.ServerReadWriteLock)
                    {
                        this.context.Server.SendResponse(
                            args.Device,
                            args.RequestId,
                            request.Status.ToNative(),
                            args.Offset,
                            request.Value
                        );
                    }
                });
                this.context.Callbacks.CharacteristicRead += handler;
                return () => this.context.Callbacks.CharacteristicRead -= handler;
            });
        }


        protected override IGattDescriptor CreateNative(Guid uuid, byte[] value)
        {
            return new GattDescriptor(this, uuid, value);
        }


        IDevice GetOrAdd(BluetoothDevice native)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(native.Address))
                    return this.subscribers[native.Address];

                var device = new Device(native);
                this.subscribers.Add(native.Address, device);
                return device;
            }
        }


        IDevice Remove(BluetoothDevice native)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(native.Address))
                {
                    var device = this.subscribers[native.Address];
                    this.subscribers.Remove(native.Address);
                    return device;
                }
                return null;
            }
        }
    }
}
