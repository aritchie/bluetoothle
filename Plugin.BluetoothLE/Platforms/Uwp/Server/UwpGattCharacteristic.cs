using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;


namespace Plugin.BluetoothLE.Server
{
    public class UwpGattCharacteristic : AbstractGattCharacteristic, IUwpGattCharacteristic
    {
        readonly Subject<GattLocalCharacteristic> nativeReady;
        readonly IList<IDevice> connectedDevices;
        GattLocalCharacteristic native;


        public UwpGattCharacteristic(IGattService service,
                                     Guid characteristicUuid,
                                     CharacteristicProperties properties,
                                     GattPermissions permissions) : base(service, characteristicUuid, properties, permissions)
        {
            this.nativeReady = new Subject<GattLocalCharacteristic>();
            this.connectedDevices = new List<IDevice>();
            this.SubscribedDevices = new ReadOnlyCollection<IDevice>(this.connectedDevices);
        }


        protected override IGattDescriptor CreateNative(Guid uuid, byte[] value)
            => new UwpGattDescriptor(this, uuid, value);


        public override IReadOnlyList<IDevice> SubscribedDevices { get; }
        public override void Broadcast(byte[] value, params IDevice[] devices)
        {
            var buffer = value.AsBuffer();
            this.native.NotifyValueAsync(buffer); // TODO: device filtering
        }


        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IDevice[] devices)
        {
            return Observable.Create<CharacteristicBroadcast>(async ob =>
            {
                var buffer = value.AsBuffer();

                var result = await this.native.NotifyValueAsync(buffer); // TODO: get clients
                //result[0].ProtocolError
                ob.OnNext(new CharacteristicBroadcast(null, this, value, false, true)); // TODO: errors and such
                ob.OnCompleted();

                return Disposable.Empty;
            });
        }


        IObservable<DeviceSubscriptionEvent> subscriptionOb;
        public override IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged()
        {
            this.subscriptionOb = this.subscriptionOb ?? Observable.Create<DeviceSubscriptionEvent>(ob =>
            {
                var handler = new TypedEventHandler<GattLocalCharacteristic, object>((sender, args) =>
                {
                    // check for dropped subscriptions
                    var copy = this.SubscribedDevices.ToList(); // copy
                    foreach (var device in copy)
                    {
                        var found = sender.SubscribedClients.Any(x => x.Session.DeviceId.Id.Equals(device.Uuid.ToString()));
                        if (!found)
                        {
                            this.connectedDevices.Remove(device);
                            ob.OnNext(new DeviceSubscriptionEvent(device, false));
                        }
                    }
                    foreach (var client in sender.SubscribedClients)
                    {
                        var dev = this.FindDevice(client.Session);
                        if (dev != null)
                        {
                            ob.OnNext(new DeviceSubscriptionEvent(dev, false)); // now have to
                            break;
                        }
                    }

                    // check for new subscriptions
                    foreach (var client in sender.SubscribedClients)
                    {
                        var dev = this.FindDevice(client.Session);
                        if (dev == null)
                        {
                            dev = this.AddConnectedDevice(client.Session);
                            ob.OnNext(new DeviceSubscriptionEvent(dev, true));
                        }
                    }
                });
                var sub = this.nativeReady.Subscribe(ch => this.native.SubscribedClientsChanged += handler);

                return () =>
                {
                    sub.Dispose();
                    if (this.native != null)
                        this.native.SubscribedClientsChanged -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.subscriptionOb;
        }


        IObservable<WriteRequest> writeOb;
        public override IObservable<WriteRequest> WhenWriteReceived()
        {
            this.writeOb = this.writeOb ?? Observable.Create<WriteRequest>(ob =>
            {
                var handler = new TypedEventHandler<GattLocalCharacteristic, GattWriteRequestedEventArgs>(async (sender, args) =>
                {
                    var request = await args.GetRequestAsync();
                    var bytes = request.Value.ToArray();
                    var respond = request.Option == GattWriteOption.WriteWithResponse;
                    ob.OnNext(new WriteRequest(null, bytes, (int)request.Offset, respond));

                    if (respond)
                    {
                        request.Respond();
                    }
                });
                var sub = this.nativeReady.Subscribe(dev => this.native.WriteRequested += handler);
                return () =>
                {
                    sub.Dispose();
                    if (this.native != null)
                        this.native.WriteRequested -= handler;
                };
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
                var handler = new TypedEventHandler<GattLocalCharacteristic, GattReadRequestedEventArgs>(async (sender, args) =>
                {
                    var request = await args.GetRequestAsync();
                    var dev = this.FindDevice(args.Session);
                    var read = new ReadRequest(dev, (int)request.Length);
                    ob.OnNext(read);

                    if (read.Status == GattStatus.Success)
                        request.RespondWithValue(read.Value.AsBuffer());
                    else
                        request.RespondWithProtocolError((byte) read.Status);
                });
                var sub = this.nativeReady.Subscribe(dev => this.native.ReadRequested += handler);
                return () =>
                {
                    sub.Dispose();
                    if (this.native != null)
                        this.native.ReadRequested -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.readOb;
        }


        public async Task Init(GattLocalService gatt)
        {
            var ch = await gatt.CreateCharacteristicAsync(
                this.Uuid,
                new GattLocalCharacteristicParameters
                {
                    CharacteristicProperties = this.ToNative(this.Properties),

                    ReadProtectionLevel = this.Permissions.HasFlag(GattPermissions.ReadEncrypted)
                        ? GattProtectionLevel.EncryptionAndAuthenticationRequired
                        : GattProtectionLevel.Plain,

                    WriteProtectionLevel =this.Permissions.HasFlag(GattPermissions.WriteEncrypted)
                        ? GattProtectionLevel.EncryptionAndAuthenticationRequired
                        : GattProtectionLevel.Plain,
                }
            );
            foreach (var descriptor in this.Descriptors.OfType<IUwpGattDescriptor>())
            {
                await descriptor.Init(ch.Characteristic);
            }
            this.native = ch.Characteristic;
            this.nativeReady.OnNext(ch.Characteristic);
        }


        protected GattCharacteristicProperties ToNative(CharacteristicProperties props)
        {
            var value = props
                .ToString()
                .Replace(CharacteristicProperties.WriteNoResponse.ToString(), GattCharacteristicProperties.WriteWithoutResponse.ToString())
                .Replace(CharacteristicProperties.NotifyEncryptionRequired.ToString(), String.Empty)
                .Replace(CharacteristicProperties.IndicateEncryptionRequired.ToString(), String.Empty);

            return (GattCharacteristicProperties)Enum.Parse(typeof(GattCharacteristicProperties), value);
        }


        protected virtual IDevice FindDevice(GattSession session)
        {
            foreach (var device in this.SubscribedDevices)
            {
                if (device.Uuid.ToString().Equals(session.DeviceId.Id))
                    return device;
            }
            return null;
        }


        protected virtual IDevice AddConnectedDevice(GattSession session)
        {
            var dev = new UwpDevice(session);
            this.connectedDevices.Add(dev);
            return dev;
        }
    }
}
