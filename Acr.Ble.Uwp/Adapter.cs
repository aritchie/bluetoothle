using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.UI.Core;


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly DeviceManager deviceManager;
        readonly Lazy<Radio> radio;
        readonly Subject<bool> scanStatusSubject;
        readonly BluetoothLEAdvertisementWatcher watcher;
        readonly DeviceWatcher deviceWatcher;


        public Adapter()
        {
            this.scanStatusSubject = new Subject<bool>();
            this.deviceManager = new DeviceManager();
            this.watcher = new BluetoothLEAdvertisementWatcher();

            // this only sees peared devices though
            this.deviceWatcher = DeviceInformation.CreateWatcher(
                GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess),
                null
            );

            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public bool IsScanning => this.watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;


        public AdapterStatus Status
        {
            get
            {
                if (this.radio == null)
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


        public IObservable<bool> WhenScanningStatusChanged()
        {
            return this.scanStatusSubject;
        }


        IObservable<IScanResult> scanner;
        public IObservable<IScanResult> Scan()
        {
            this.scanner = this.scanner ?? Observable.Create<IScanResult>(ob =>
            {
                var sub = this.ScanListen().Subscribe(ob.OnNext);
                this.watcher.Start();
                this.deviceWatcher.Start();
                this.scanStatusSubject.OnNext(true);

                return () =>
                {
                    this.watcher.Stop();
                    this.deviceWatcher.Stop();
                    this.scanStatusSubject.OnNext(false);
                    sub.Dispose();
                };
            });
            return this.scanner;
        }


        public IObservable<IScanResult> BackgroundScan(Guid serviceUuid)
        {
            return this
                .Scan()
                .Where(x => x
                    .AdvertisementData
                    .ServiceUuids?
                    .Any(uuid => uuid.Equals(serviceUuid)) ?? false
                );
        }


        IObservable<IScanResult> scanListenOb;
        public IObservable<IScanResult> ScanListen()
        {
            this.scanListenOb = this.scanListenOb ?? Observable.Create<IScanResult>(ob =>
            {
                var adHandler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                (
                    (sender, args) =>
                    {
                        IDevice device = null;
                        if (!this.deviceManager.TryGetDevice(args.BluetoothAddress, out device))
                            return;

                        var adData = new AdvertisementData(args);
                        var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                        ob.OnNext(scanResult);
                    }
                );
                var deviceHandler = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (sender, args) =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            var native = await GattDeviceService.FromIdAsync(args.Id);
                            this.deviceManager.GetDevice(native);
                        }
                    );
                });

                this.watcher.Received += adHandler;
                this.deviceWatcher.Added += deviceHandler;

                return () =>
                {
                    this.watcher.Received -= adHandler;
                    this.deviceWatcher.Added -= deviceHandler;
                };
            })
            .Publish()
            .RefCount();

            return this.scanListenOb;
        }


        IObservable<AdapterStatus> statusOb;
        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ??Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                this.radio.Value.StateChanged += handler;
                return () => this.radio.Value.StateChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.statusOb;
        }


        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            return Observable.Empty<IDevice>(); // TODO
        }
    }
}
