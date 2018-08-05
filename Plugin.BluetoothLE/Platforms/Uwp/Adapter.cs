using Plugin.BluetoothLE.Server;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.System;
using Windows.Devices.Enumeration;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly AdapterContext context = new AdapterContext();
        BluetoothAdapter native;
        Radio radio;


        public Adapter()
        {
            this.Advertiser = new Advertiser();
        }


        public Adapter(BluetoothAdapter native, Radio radio) : this()
        {
            this.native = native;
            this.radio = radio;
            this.DeviceName = radio.Name;
        }


        public override AdapterFeatures Features
        {
            get
            {
                if (this.native == null || !this.native.IsLowEnergySupported)
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


        public override bool IsScanning { get; protected set; }


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


        public override IObservable<IDevice> GetKnownDevice(Guid deviceId) => Observable.FromAsync(async ct =>
        {
            var mac = deviceId.ToBluetoothAddress();
            var device = this.context.GetDevice(mac);

            if (device == null)
            {
                var native = await BluetoothLEDevice.FromBluetoothAddressAsync(mac).AsTask(ct);
                device = this.context.AddDevice(native);
            }

            return device;
        });


        public override IObservable<IEnumerable<IDevice>> GetConnectedDevices() => this.GetDevices(
            BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
        );


        public override IObservable<IEnumerable<IDevice>> GetPairedDevices() => this.GetDevices(
            BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)
        );


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return Observable.Create<IScanResult>(ob =>
            {
                this.IsScanning = true;

                var sub = this
                    .WhenRadioReady()
                    .Where(rdo => rdo != null)
                    .Select(_ => this.CreateScanner(config))
                    .Switch()
                    .Subscribe(
                        async args => // CAREFUL
                        {
                            var device = this.context.GetDevice(args.BluetoothAddress);
                            if (device == null)
                            {
                                var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                                if (btDevice != null)
                                    device = this.context.AddDevice(btDevice);
                            }
                            if (device != null)
                            {
                                var adData = new AdvertisementData(args);
                                var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                                ob.OnNext(scanResult);
                            }
                        },
                        ob.OnError
                    );

                return () =>
                {
                    this.IsScanning = false;
                    sub.Dispose();
                };
            });
        }


        public override void StopScan()
        {
            // TODO
        }


        IObservable<AdapterStatus> statusOb;
        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<AdapterStatus>(ob =>
            {
                Radio r = null;
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                var sub = this
                    .WhenRadioReady()
                    .Subscribe(
                        rdo =>
                        {
                            r = rdo;
                            ob.OnNext(this.Status);
                            r.StateChanged += handler;
                        },
                        ob.OnError
                    );

                return () =>
                {
                    sub.Dispose();
                    if (r != null)
                        r.StateChanged -= handler;
                };
            })
            .Publish()
            .StartWith(this.Status)
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public override IGattServer CreateGattServer() => new GattServer();


        //IObservable<IDevice> deviceStatusOb;
        //public override IObservable<IDevice> WhenDeviceStatusChanged()
        //{
        //    this.deviceStatusOb = this.deviceStatusOb ?? Observable.Create<IDevice>(ob =>
        //    {
        //        var cleanup = new List<IDisposable>();
        //        var devices = this.context.GetDiscoveredDevices();

        //        foreach (var device in devices)
        //        {
        //            cleanup.Add(device
        //                .WhenStatusChanged()
        //                .Subscribe(_ => ob.OnNext(device))
        //            );
        //        }
        //        return () => cleanup.ForEach(x => x.Dispose());
        //    });
        //    return this.deviceStatusOb;
        //}


        public override async void OpenSettings()
            => await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));


        public override async void SetAdapterState(bool enable)
        {
            var state = enable ? RadioState.On : RadioState.Off;
            await this.radio.SetStateAsync(state);
        }


        IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateScanner(ScanConfig config)
             => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
             {
                 this.context.Clear();
                 config = config ?? new ScanConfig { ScanType = BleScanType.Balanced };

                 var adWatcher = new BluetoothLEAdvertisementWatcher();
                 if (config.ServiceUuids != null)
                     foreach (var serviceUuid in config.ServiceUuids)
                         adWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceUuid);

                 switch (config.ScanType)
                 {
                     case BleScanType.Balanced:
                         adWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                         break;

                     case BleScanType.Background:
                     case BleScanType.LowLatency:
                     case BleScanType.LowPowered:
                         adWatcher.ScanningMode = BluetoothLEScanningMode.Passive;
                         break;
                 }
                 var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                     ((sender, args) => ob.OnNext(args)
                 );

                 adWatcher.Received += handler;
                 adWatcher.Start();

                 return () =>
                 {
                     adWatcher.Stop();
                     adWatcher.Received -= handler;
                 };
             });


        IObservable<Radio> WhenRadioReady() => Observable.FromAsync(async ct =>
        {
            if (this.radio != null)
                return this.radio;

            this.native = await BluetoothAdapter.GetDefaultAsync().AsTask(ct);
            if (this.native == null)
                throw new ArgumentException("No bluetooth adapter found");

            this.radio = await this.native.GetRadioAsync().AsTask(ct);
            return this.radio;
        });


        IObservable<IEnumerable<IDevice>> GetDevices(string selector) => Observable.FromAsync(async ct =>
        {
            string[] requestedProperties = { "System.Devices.ContainerId" };
            var devices = await DeviceInformation
                .FindAllAsync(
                    selector,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint
                )
                .AsTask(ct);

            var results = new List<IDevice>();
            foreach (var deviceInfo in devices)
            {
                var native = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id).AsTask(ct);
                var wrap = this.context.AddOrGetDevice(native);
                results.Add(wrap);
            }

            return results;
        });
    }
}