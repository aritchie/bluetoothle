using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Foundation;
using Windows.Devices.Radios;
using Windows.System;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    //BluetoothAdapter.IsPeripheralRoleSupported
    public class Adapter : AbstractAdapter
    {
        readonly BleContext context = new BleContext();
        readonly Subject<bool> scanStatusSubject = new Subject<bool>();
        BluetoothAdapter native;
        Radio radio;


        public Adapter()
        {
        }


        public Adapter(BluetoothAdapter native, Radio radio)
        {
            this.native = native;
            this.radio = radio;
        }


        public override string DeviceName => this.radio?.Name;


        public override AdapterFeatures Features
        {
            get
            {
                if (!this.native.IsLowEnergySupported)
                    return AdapterFeatures.None;

                var features = AdapterFeatures.AllClient;
                if (!this.native.IsCentralRoleSupported)
                    features &= ~AdapterFeatures.AllClient;

                if (this.native.IsPeripheralRoleSupported)
                    features |= AdapterFeatures.AllServer;

                if (this.native.IsCentralRoleSupported || this.native.IsPeripheralRoleSupported)
                    features |= AdapterFeatures.AllControls;

                return features;
            }
        }


        public override IGattServer CreateGattServer() => new GattServer();


        bool isScanning = false;
        public override bool IsScanning
        {
            get => this.isScanning;
            protected set
            {
                if (this.isScanning == value)
                    return;

                this.isScanning = value;
                this.scanStatusSubject.OnNext(value);
            }
        }


        public override AdapterStatus Status
        {
            get
            {
                if (this.radio == null)
                    return AdapterStatus.Unknown;

                switch (this.radio.State)
                {
                    case RadioState.Disabled:
                    case RadioState.Off:
                        return AdapterStatus.PoweredOff;

                    case RadioState.Unknown:
                        return AdapterStatus.Unknown;

                    default:
                        return AdapterStatus.PoweredOn;
                }
            }
        }


        public override IEnumerable<IDevice> GetConnectedDevices() => this.context.GetConnectedDevices();


        public override IObservable<bool> WhenScanningStatusChanged()
            => this.scanStatusSubject
                .AsObservable()
                .StartWith(this.IsScanning);


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            var observer = Observable.Create<IScanResult>(async ob =>
            {
                await this.EnsureRadio();

                var sub = this.ScanListen().Subscribe(ob.OnNext);
                this.IsScanning = true; // this will actually fire off the scanner

                return () =>
                {
                    this.IsScanning = false;
                    sub.Dispose();
                };
            });
            if (config?.ServiceUuid != null)
            {
                observer = observer.Where(x => x.AdvertisementData?.ServiceUuids.Contains(config.ServiceUuid.Value) ?? false);
            }

            return observer;
        }


        IObservable<IScanResult> scanListenOb;
        public override IObservable<IScanResult> ScanListen()
        {
            IDisposable adWatcher = null;
            IDisposable devWatcher = null;

            this.scanListenOb = this.scanListenOb ?? Observable.Create<IScanResult>(ob =>
                this.WhenScanningStatusChanged().Subscribe(scan =>
                {
                    if (!scan)
                    {
                        adWatcher?.Dispose();
                    }
                    else
                    {
                        this.context.Clear();

                        // TODO: this will only capture fully advertised devices
                        adWatcher = this.context
                            .CreateAdvertisementWatcher()
                            .Subscribe(async args => // CAREFUL
                            {
                                var device = this.context.GetDevice(args.BluetoothAddress);
                                if (device == null)
                                {
                                    var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                                    if (btDevice != null)
                                        device = this.context.AddDevice(args.BluetoothAddress, btDevice);
                                }
                                if (device != null)
                                {
                                    var adData = new AdvertisementData(args);
                                    var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                                    ob.OnNext(scanResult);
                                }
                            });
                    }
                })
            )
            .Publish()
            .RefCount();

            return this.scanListenOb;
        }


        IObservable<AdapterStatus> statusOb;
        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<AdapterStatus>(async ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                await this.EnsureRadio();
                this.radio.StateChanged += handler;

                return () => this.radio.StateChanged -= handler;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        IObservable<IDevice> deviceStatusOb;
        public override IObservable<IDevice> WhenDeviceStatusChanged()
        {
            this.deviceStatusOb = this.deviceStatusOb ?? Observable.Create<IDevice>(ob =>
            {
                var cleanup = new List<IDisposable>();
                var devices = this.context.GetDiscoveredDevices();

                foreach (var device in devices)
                {
                    cleanup.Add(device
                        .WhenStatusChanged()
                        .Subscribe(_ => ob.OnNext(device))
                    );
                }
                return () => cleanup.ForEach(x => x.Dispose());
            });
            return this.deviceStatusOb;
        }


        public override async void OpenSettings()
            => await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));


        public override async void SetAdapterState(bool enable)
        {
            var state = enable ? RadioState.On : RadioState.Off;
            await this.radio.SetStateAsync(state);
        }


        async Task EnsureRadio()
        {
            if (this.radio != null)
                return;

            this.native = await BluetoothAdapter.GetDefaultAsync();
            if (this.native == null)
                throw new ArgumentException("No bluetooth adapter found");

            this.radio = await this.native.GetRadioAsync();
        }
    }
}