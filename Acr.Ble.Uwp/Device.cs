using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Plugin.BluetoothLE
{
    public class Device : IDevice
    {
        readonly BleContext context;
        readonly BluetoothLEDevice native;
        readonly Subject<ConnectionStatus> connSubject;


        public Device(BleContext context, BluetoothLEDevice native)
        {
            this.context = context;
            this.native = native;

            var mac = this.ToMacAddress(native.BluetoothAddress);
            this.Uuid = this.GetDeviceId(mac);
            //this.Uuid = this.GetDeviceId(native.DeviceId);
        }


        public string Name => this.native.Name;
        public Guid Uuid { get; }
        public DeviceFeatures Features => DeviceFeatures.PairingRequests | DeviceFeatures.ReliableTransactions;


        public IGattReliableWriteTransaction BeginReliableWriteTransaction()
        {
            return new GattReliableWriteTransaction();
        }


        public IObservable<object> Connect(GattConnectionConfig config)
        {
            // TODO: configurable "connection" type - RSSI check, timed read on first characteristic, device watcher
            // TODO: monitor devicewatcher - if removed d/c, if added AND paired - connected
            this.connSubject.OnNext(ConnectionStatus.Connected);
            this.status = ConnectionStatus.Connected;
            return Observable.Return(new object());
        }


        public void CancelConnection()
        {
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
            this.status = ConnectionStatus.Disconnected;
            // TODO: kill RSSI, devicewatcher
            // TODO: kill all characteristics
        }


        ConnectionStatus status = ConnectionStatus.Disconnected;
        public ConnectionStatus Status => this.status;
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


        IObservable<ConnectionStatus> statusOb;
        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            // TODO: monitor devicewatcher - if removed d/c, if added AND paired - connected
            // TODO: shut devicewatcher off if characteristic hooked?
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                this.native.ConnectionStatusChanged += handler;

                return () => this.native.ConnectionStatusChanged -= handler;
            })
            .Replay(1);

            return this.statusOb;
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            return this.context
                .CreateAdvertisementWatcher()
                .Where(x => x.BluetoothAddress == this.native.BluetoothAddress)
                .Select(x => (int)x.RawSignalStrengthInDBm);
        }


        IObservable<IGattService> serviceOb;
        public IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
                this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(async x =>
                    {
                        var result = await this.native.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                        foreach (var nservice in result.Services)
                        {
                            var service = new GattService(nservice, this);
                            ob.OnNext(service);
                        }
                    })
            )
            .ReplayWithReset(this.WhenStatusChanged()
                .Skip(1)
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.serviceOb;
        }


        IObservable<string> nameOb;
        public IObservable<string> WhenNameUpdated()
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


        public PairingStatus PairingStatus => this.native.DeviceInformation.Pairing.IsPaired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public IObservable<bool> PairingRequest(string pin = null)
        {
            return Observable.Create<bool>(async ob =>
            {
                var result = await this.native.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                var status = result.Status == DevicePairingResultStatus.Paired;
                ob.Respond(status);
                return Disposable.Empty;
            });
        }


        public IObservable<int> RequestMtu(int size)
        {
            return Observable.Return(20); // TODO
        }


        public int GetCurrentMtuSize()
        {
            return 20;
        }


        public IObservable<int> WhenMtuChanged()
        {
            return Observable.Return(this.GetCurrentMtuSize());
        }


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