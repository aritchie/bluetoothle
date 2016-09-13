using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Foundation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly DeviceManager deviceManager;
        readonly Lazy<Radio> radio;
        readonly Subject<bool> scanStatus;


        public Adapter()
        {
            this.scanStatus = new Subject<bool>();
            this.deviceManager = new DeviceManager();
            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public bool IsScanning { get; private set; }
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
            return this.scanStatus;
        }


        IObservable<IScanResult> scanner;
        public IObservable<IScanResult> Scan()
        {
            this.scanner = this.scanner ?? Observable.Create<IScanResult>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>(
                    async (sender, args) =>
                    {
                        var device = await this.deviceManager.GetDevice(args);
                        var adData = new AdvertisementData(args.Advertisement);
                        var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                        ob.OnNext(scanResult);
                    }
                );

                var watcher = new BluetoothLEAdvertisementWatcher();
                watcher.Received += handler;
                watcher.Start();

                this.SetScanStatus(true);

                return () =>
                {
                    watcher.Stop();
                    watcher.Received -= handler;
                    this.SetScanStatus(false);
                };
            });
            return this.scanner;
        }


        public IObservable<IScanResult> BackgroundScan(Guid serviceUuid)
        {
            return null;
        }


        public IObservable<IScanResult> ScanListen()
        {
            return null;
        }


        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            return Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                this.radio.Value.StateChanged += handler;
                return () => this.radio.Value.StateChanged -= handler;
            });
        }


        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            return Observable.Empty<IDevice>(); // TODO
        }


        protected virtual void SetScanStatus(bool isScanning)
        {
            this.scanStatus.OnNext(isScanning);
            this.IsScanning = isScanning;
        }
    }
}
