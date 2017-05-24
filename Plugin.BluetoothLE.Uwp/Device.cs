using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly BleContext context;
        readonly Subject<ConnectionStatus> connSubject;
        BluetoothLEDevice native;


        public Device(BleContext context, BluetoothLEDevice native)
        {
            this.connSubject = new Subject<ConnectionStatus>();
            this.context = context;
            this.native = native;

            var mac = this.ToMacAddress(native.BluetoothAddress);
            this.Uuid = this.GetDeviceId(mac);
            //this.Uuid = this.GetDeviceId(native.DeviceId);
        }


        public override string Name => this.native.Name;
        public override object NativeDevice => this.native;
        public override DeviceFeatures Features => DeviceFeatures.PairingRequests | DeviceFeatures.ReliableTransactions;


        public override IGattReliableWriteTransaction BeginReliableWriteTransaction()
            => new GattReliableWriteTransaction();


        public override IObservable<object> Connect(GattConnectionConfig config)
            => Observable.FromAsync(async token =>
            {
                //if (this.native == null)
                    //this.native = await BluetoothLEDevice.FromIdAsync(this.deviceId);

                //this.native = await BluetoothLEDevice.FromBluetoothAddressAsync(0L);
                this.status = ConnectionStatus.Connected;
                this.connSubject.OnNext(ConnectionStatus.Connected);
                return new object();
            });

            // TODO: configurable "connection" type - RSSI check, timed read on first characteristic, device watcher
            // TODO: monitor devicewatcher - if removed d/c, if added AND paired - connected


        public override async void CancelConnection()
        {
            if (this.native == null)
                return;

            this.connSubject.OnNext(ConnectionStatus.Disconnected);
            this.status = ConnectionStatus.Disconnected;

            var ns = await this.native.GetGattServicesAsync(BluetoothCacheMode.Cached);
            foreach (var nservice in ns.Services)
            {
                var nch = await nservice.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
                var tcs = new TaskCompletionSource<object>();
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(
                    CoreDispatcherPriority.High,
                    async () =>
                    {
                        foreach (var characteristic in nch.Characteristics)
                        {
                            if (!characteristic.HasNotify())
                                return;

                            try
                            {
                                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                            }
                            catch (Exception e)
                            {
                                //System.Console.WriteLine(e);
                                System.Diagnostics.Debug.WriteLine(e.ToString());
                            }
                        }
                        tcs.TrySetResult(null);
                    }
                );
                await tcs.Task;
                nservice.Dispose();
            }
            this.native.Dispose();
            this.native = null;
        }


        ConnectionStatus status = ConnectionStatus.Disconnected;
        public override ConnectionStatus Status => this.status;
        //{
        //    get
        //    {
        // TODO: monitor devicewatcher - if removed d/c, if added AND paired - connected
        //switch (this.native.ConnectionStatus)
        //{
        //    case BluetoothConnectionStatus.Connected:
        //        return ConnectionStatus.Connected;

        //    default:
        //        return ConnectionStatus.Disconnected;
        //}
        //    }
        //}


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => Observable.FromAsync(async ct =>
            {
                var result = await this.native.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Cached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("Could not find GATT service - " + result.Status);

                var wrap = new GattService(result.Services.First(), this);
                return wrap;
            });


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged() => this.connSubject;
        //{
        //    // TODO: monitor devicewatcher - if removed d/c, if added AND paired - connected
        //    // TODO: shut devicewatcher off if characteristic hooked?
        //    this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
        //    {
        //        ob.OnNext(this.Status);
        //        var handler = new TypedEventHandler<BluetoothLEDevice, object>(
        //            (sender, args) => ob.OnNext(this.Status)
        //        );
        //        this.native.ConnectionStatusChanged += handler;

        //        return () => this.native.ConnectionStatusChanged -= handler;
        //    })
        //    .Replay(1);

        //    return this.statusOb;
        //}


        public override IObservable<int> WhenRssiUpdated(TimeSpan? frequency)
            => this.context
                .CreateAdvertisementWatcher()
                .Where(x => x.BluetoothAddress == this.native.BluetoothAddress)
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
                this.native.GattServicesChanged += handler;

                var sub = this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Select(_ => Observable.FromAsync(async ct =>
                    {
                        var result = await this.native.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                        foreach (var nservice in result.Services)
                        {
                            var service = new GattService(nservice, this);
                            ob.OnNext(service);
                        }
                    }))
                    .Merge()
                    .Subscribe();

                return () =>
                {
                    sub.Dispose();
                    this.native.GattServicesChanged -= handler;
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
                this.native.NameChanged += handler;

                return () => this.native.NameChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public override PairingStatus PairingStatus => this.native.DeviceInformation.Pairing.IsPaired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public override IObservable<bool> PairingRequest(string pin = null)
            => Observable.FromAsync(async token =>
            {
                var result = await this.native.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
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