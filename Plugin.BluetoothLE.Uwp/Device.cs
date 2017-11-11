using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly AdapterContext adapterContext;
        readonly DeviceContext context;


        public Device(AdapterContext context, BluetoothLEDevice native)
        {
            this.adapterContext = context;
            this.context = new DeviceContext(this, native);

            var mac = this.ToMacAddress(native.BluetoothAddress);
            this.Uuid = this.GetDeviceId(mac);
            //this.Uuid = this.GetDeviceId(native.DeviceId);
        }


        public override string Name => this.context.NativeDevice.Name;
        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.PairingRequests | DeviceFeatures.ReliableTransactions;
        public override IGattReliableWriteTransaction BeginReliableWriteTransaction() => new GattReliableWriteTransaction();


        public override IObservable<object> Connect(GattConnectionConfig config)
            => Observable.Create<object>(ob =>
            {
                var sub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ => ob.Respond(null));

                this.context.Connect();

                return sub;
            });


        public override async void CancelConnection() => await this.context.Disconnect();


        public override ConnectionStatus Status
        {
            get
            {
                switch (this.context.NativeDevice.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => Observable.FromAsync(async ct =>
            {
                var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Cached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("Could not find GATT service - " + result.Status);

                var wrap = new GattService(this.context, result.Services.First());
                return wrap;
            });


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                this.context.NativeDevice.ConnectionStatusChanged += handler;

                return () => this.context.NativeDevice.ConnectionStatusChanged -= handler;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? frequency)
            => this.adapterContext
                .CreateAdvertisementWatcher(null)
                .Where(x => x.BluetoothAddress == this.context.NativeDevice.BluetoothAddress)
                .Select(x => (int)x.RawSignalStrengthInDBm);


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEDevice, object>((sender, args) =>
                {
                    //if (this.native.Equals(sender))
                });
                this.context.NativeDevice.GattServicesChanged += handler;

                var sub = this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Select(_ => Observable.FromAsync(async ct =>
                    {
                        var result = await this.context.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                        foreach (var nservice in result.Services)
                        {
                            var service = new GattService(this.context, nservice);
                            ob.OnNext(service);
                        }
                    }))
                    .Merge()
                    .Subscribe();

                return () =>
                {
                    sub.Dispose();
                    this.context.NativeDevice.GattServicesChanged -= handler;
                };
            })
            .ReplayWithReset(this.WhenStatusChanged()
                .Skip(1)
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.serviceOb;
        }


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                this.context.NativeDevice.NameChanged += handler;

                return () => this.context.NativeDevice.NameChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public override PairingStatus PairingStatus => this.context.NativeDevice.DeviceInformation.Pairing.IsPaired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public override IObservable<bool> PairingRequest(string pin = null)
            => Observable.FromAsync(async token =>
            {
                var result = await this.context.NativeDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                var state = result.Status == DevicePairingResultStatus.Paired;
                return state;
            });


        static readonly Regex macRegex = new Regex("(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})");
        const string REGEX_REPLACE = "$1:$2:$3:$4:$5:$6";


        string ToMacAddress(ulong address)
        {
            var tempMac = address.ToString("X");
            //tempMac is now 'E7A1F7842F17'

            //string.Join(":", BitConverter.GetBytes(BluetoothAddress).Reverse().Select(b => b.ToString("X2"))).Substring(6);
            var leadingZeros = new string('0', 12 - tempMac.Length);
            tempMac = leadingZeros + tempMac;

            var macAddress = macRegex.Replace(tempMac, REGEX_REPLACE);
            return macAddress;
        }


        protected Guid GetDeviceId(string address)
        {
            var mac = address
                .Replace("BluetoothLE#BluetoothLE", String.Empty)
                .Replace(":", String.Empty)
                .Replace("-", String.Empty);

            var deviceGuid = new byte[16];
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();

            macBytes.CopyTo(deviceGuid, 10); // 12 bytes here if off the BluetoothLEDevice
            return new Guid(deviceGuid);
        }
    }
}