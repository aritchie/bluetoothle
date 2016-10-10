using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Acr.Ble
{
    public class Device : IDevice
    {
        readonly AdvertisementData adData;
        readonly Subject<bool> deviceSubject;
        GattDeviceService native;


        public Device(AdvertisementData adData)
        {
            this.adData = adData;
            this.deviceSubject = new Subject<bool>();

            this.Name = adData.Native.GetDeviceName();
            var mac = this.ToMacAddress(adData.BluetoothAddress);
            var uuid = this.ToDeviceId(mac);
            this.Uuid = uuid;
        }


        public string Name { get; }
        public Guid Uuid { get; }


        public IObservable<ConnectionStatus> CreateConnection()
        {
            return Observable.Create<ConnectionStatus>(async ob =>
            {
                var status = this
                    .WhenStatusChanged()
                    .Subscribe(ob.OnNext);
                // TODO: reconnect
                await this.Connect();

                return status;
            });
        }


        public IObservable<object> Connect()
        {
            return Observable.Create<object>(async ob =>
            {
                if (this.Status == ConnectionStatus.Connected)
                {
                    ob.Respond(null);
                }
                else
                {
                    // TODO: connecting
                    var selector = BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(this.adData.BluetoothAddress);
                    var devices = await DeviceInformation.FindAllAsync(selector);
                    var devInfo = devices.FirstOrDefault();
                    if (devInfo == null)
                        throw new ArgumentException("Device Not Found");

                    if (devInfo.Pairing.CanPair && !devInfo.Pairing.IsPaired)
                    {
                        var dpr = await devInfo.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                        if (dpr.Status != DevicePairingResultStatus.Paired)
                            throw new ArgumentException($"Pairing to device failed - " + dpr.Status);
                    }
                    this.native = await GattDeviceService.FromIdAsync(devInfo.Id);
                    ob.Respond(null);
                    this.deviceSubject.OnNext(true);
                }
                return Disposable.Empty;
            });
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            return Observable.Empty<int>();
        }


        public void Disconnect()
        {
            if (this.Status != ConnectionStatus.Connected)
                return;

            this.native?.Dispose();
            this.native = null;
        }


        public ConnectionStatus Status
        {
            get
            {
                if (this.native == null)
                    return ConnectionStatus.Disconnected;

                switch (this.native.Device.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        IObservable<ConnectionStatus> statusOb;
        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );

                var sub = this.deviceSubject
                    .AsObservable()
                    .Subscribe(x =>
                    {
                        ob.OnNext(this.Status);
                        if (this.native != null)
                            this.native.Device.ConnectionStatusChanged += handler;
                    });

                return () =>
                {
                    sub.Dispose();
                    if (this.native != null)
                        this.native.Device.ConnectionStatusChanged -= handler;
                };
            })
            .Replay(1);

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
                this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(x =>
                    {
                        foreach (var nservice in this.native.Device.GattServices)
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
                var sub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(x => this.native.Device.NameChanged += handler);

                return () =>
                {
                    sub.Dispose();
                    if (this.native != null)
                        this.native.Device.NameChanged -= handler;
                };
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        string ToMacAddress(ulong address)
        {
            var tempMac = address.ToString("X");
            //tempMac is now 'E7A1F7842F17'

            //string.Join(":", BitConverter.GetBytes(BluetoothAddress).Reverse().Select(b => b.ToString("X2"))).Substring(6);
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";
            var macAddress = Regex.Replace(tempMac, regex, replace);
            return macAddress;
        }


        Guid ToDeviceId(string address)
        {
            var deviceGuid = new byte[16];
            var mac = address.Replace(":", "");
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();

            macBytes.CopyTo(deviceGuid, 10);
            return new Guid(deviceGuid);
        }
    }
}