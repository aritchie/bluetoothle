using System;
using System.Collections.Generic;
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
        readonly Subject<bool> scanStatusSubject;
        readonly BluetoothLEAdvertisementWatcher watcher;


        public Adapter()
        {
            this.scanStatusSubject = new Subject<bool>();
            this.deviceManager = new DeviceManager(this);
            this.watcher = new BluetoothLEAdvertisementWatcher();

            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public bool IsScanning => this.watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.deviceManager.GetConnectedDevices();
        }


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

            return Observable.Create<bool>(ob =>
            {
                ob.OnNext(this.IsScanning);
                return this.scanStatusSubject.AsObservable().Subscribe(ob.OnNext);
            });
        }


        IObservable<IScanResult> scanner;
        public IObservable<IScanResult> Scan()
        {
            this.scanner = this.scanner ?? Observable.Create<IScanResult>(ob =>
            {
                var sub = this.ScanListen().Subscribe(ob.OnNext);
                this.watcher.Start();
                this.scanStatusSubject.OnNext(true);

                return () =>
                {
                    this.watcher.Stop();
                    this.scanStatusSubject.OnNext(false);
                    sub.Dispose();
                };
            })
            .Publish()
            .RefCount();

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
                this.deviceManager.Clear();

                var adHandler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                (
                    (sender, args) =>
                    {
                        var adData = new AdvertisementData(args);
                        var device = this.deviceManager.GetDevice(adData);
                        var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                        ob.OnNext(scanResult);
                    }
                );
                this.watcher.Received += adHandler;

                return () => this.watcher.Received -= adHandler;
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


        public bool CanOpenSettings => false;

        public void OpenSettings()
        {
        }


        public bool CanChangeAdapterState => false;

        public void SetAdapterState(bool enable)
        {
        }
    }
}
