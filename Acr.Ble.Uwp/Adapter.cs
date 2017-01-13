using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Foundation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.System;


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        const string AqsFilter = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
        static readonly string[] requestProperites = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

        readonly DeviceManager deviceManager;
        readonly Lazy<Radio> radio;
        readonly Subject<bool> scanStatusSubject;
        readonly BluetoothLEAdvertisementWatcher adWatcher;
        readonly DeviceWatcher deviceWatcher;


        public Adapter()
        {
            this.scanStatusSubject = new Subject<bool>();
            this.deviceManager = new DeviceManager(this);
            this.adWatcher = new BluetoothLEAdvertisementWatcher();
            this.deviceWatcher = DeviceInformation.CreateWatcher(AqsFilter, requestProperites, DeviceInformationKind.AssociationEndpoint);

            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public bool IsScanning => this.adWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;


        public AdapterStatus Status
        {
            get
            {
                if (this.radio.Value == null)
                    return AdapterStatus.Unsupported;

                switch (this.radio.Value.State)
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


        public IDevice GetKnownDevice(Guid deviceId)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.deviceManager.GetConnectedDevices();
        }


        public IObservable<bool> WhenScanningStatusChanged()
        {
            return Observable.Create<bool>(ob =>
            {
                ob.OnNext(this.IsScanning);
                return this.scanStatusSubject.AsObservable().Subscribe(ob.OnNext);
            });
        }


        public IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return Observable.Create<IScanResult>(ob =>
            {
                var sub = this.ScanListen().Subscribe(ob.OnNext);
                //this.adWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                this.adWatcher.Start();
                this.scanStatusSubject.OnNext(true);

                return () =>
                {
                    this.adWatcher.Stop();
                    this.scanStatusSubject.OnNext(false);
                    sub.Dispose();
                };
            });
            //        .Where(x => x
            //            .AdvertisementData
            //            .ServiceUuids?
            //            .Any(uuid => uuid.Equals(serviceUuid)) ?? false
            //        );
        }


        IObservable<IScanResult> scanListenOb;
        public IObservable<IScanResult> ScanListen()
        {
            this.scanListenOb = this.scanListenOb ?? Observable.Create<IScanResult>(ob =>
            {
                var syncLock = new object();
                var nativeDevices = new Dictionary<string, DeviceInformation>();
                this.deviceManager.Clear();

                // TODO: when a device and ad are available, device discovered
                var adHandler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                (
                    (sender, args) =>
                    {
                        //var adData = new AdvertisementData(args);
                        //var device = this.deviceManager.GetDevice(adData);
                        //var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                        //ob.OnNext(scanResult);
                    }
                );

                var addHandler = new TypedEventHandler<DeviceWatcher, DeviceInformation>((sender, args) =>
                {
                    lock (syncLock)
                    {
                        nativeDevices.Add(args.Id, args);
                    }
                });
                var remHandler = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((sender, args) =>
                {
                    lock (syncLock)
                    {
                        nativeDevices.Remove(args.Id);
                    }
                });

                this.deviceWatcher.Added += addHandler;
                this.deviceWatcher.Removed += remHandler;
                this.adWatcher.Received += adHandler;

                this.adWatcher.Start();
                this.deviceWatcher.Start();

                return () =>
                {
                    this.adWatcher.Stop();
                    this.deviceWatcher.Stop();

                    this.deviceWatcher.Added -= addHandler;
                    this.deviceWatcher.Removed -= remHandler;
                    this.adWatcher.Received -= adHandler;
                };
            })
            .Publish()
            .RefCount();

            return this.scanListenOb;
        }


        IObservable<AdapterStatus> statusOb;
        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                this.radio.Value.StateChanged += handler;
                return () => this.radio.Value.StateChanged -= handler;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            return Observable.Empty<IDevice>(); // TODO
        }


        public bool CanOpenSettings => true;

        public async void OpenSettings()
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
        }


        public bool CanChangeAdapterState => true;

        public async void SetAdapterState(bool enable)
        {
            var state = enable ? RadioState.On : RadioState.Off;
            await this.radio.Value.SetStateAsync(state);
        }


        public IObservable<IDevice> WhenDeviceStateRestored()
        {
            return Observable.Empty<IDevice>();
        }
    }
}
