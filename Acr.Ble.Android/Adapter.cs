using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Acr.Ble.Internals;
using Android.App;
using Android.Bluetooth;
using Android.OS;


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly BluetoothManager manager;
        readonly BleContext context;
        readonly Subject<bool> scanStatusChanged;


        public Adapter()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Application.BluetoothService);
            this.context = new BleContext(this.manager);
            this.scanStatusChanged = new Subject<bool>();
        }


        public bool ForcePreLollipopScanner { get; set; }
        public bool IsScanning => this.manager.Adapter.IsDiscovering;


        public AdapterStatus Status
        {
            get
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2)
                    return AdapterStatus.Unsupported;

                //this.context.AppContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe)
                if (this.manager?.Adapter == null)
                    return AdapterStatus.Unsupported;

                if (!this.manager.Adapter.IsEnabled)
                    return AdapterStatus.PoweredOff;

                switch (this.manager.Adapter.State)
                {
                    case State.Off:
                    case State.TurningOff:
                    case State.Disconnecting:
                    case State.Disconnected:
                        return AdapterStatus.PoweredOff;

                    //case State.Connecting
                    case State.On:
                    case State.Connected:
                        return AdapterStatus.PoweredOn;

                    default:
                        return AdapterStatus.Unknown;
                }
            }
        }


        IObservable<AdapterStatus> statusOb;
        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var aob = BluetoothObservables
                    .WhenAdapterStatusChanged()
                    .Subscribe(_ => ob.OnNext(this.Status));

                return aob.Dispose;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public IObservable<bool> WhenScanningStatusChanged()
        {
            return Observable.Create<bool>(ob =>
            {
                ob.OnNext(this.IsScanning);
                return this.scanStatusChanged
                    .AsObservable()
                    .Subscribe(ob.OnNext);
            });
        }


        IObservable<IScanResult> scanner;
        public IObservable<IScanResult> Scan()
        {
            this.scanner = this.scanner ?? this.CreateScanner(null);
            return this.scanner;
        }


        IObservable<IScanResult> bgScanner;
        public IObservable<IScanResult> BackgroundScan(Guid serviceUuid)
        {
            this.bgScanner = this.bgScanner ?? this.CreateScanner(serviceUuid);
            return this.bgScanner;
        }


        IObservable<IScanResult> scanListenOb;
        public IObservable<IScanResult> ScanListen()
        {
            this.scanListenOb = this.scanListenOb ?? Observable.Create<IScanResult>(ob =>
            {
                var handler = new EventHandler<ScanEventArgs>((sender, args) =>
                {
                    var dev = this.context.Devices.GetDevice(args.Device, TaskScheduler.Current);
                    ob.OnNext(new ScanResult(dev, args.Rssi, args.AdvertisementData));
                });
                this.context.Scanned += handler;
                return () => this.context.Scanned -= handler;
            })
            .Publish()
            .RefCount();

            return this.scanListenOb;
        }


        IObservable<IDevice> deviceStatusOb;
        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            this.deviceStatusOb = this.deviceStatusOb ?? Observable.Create<IDevice>(ob =>
            {
                var handler = new EventHandler<ConnectionStateEventArgs>((sender, args) =>
                {
                    var dev = this.context.Devices.GetDevice(args.Gatt.Device, TaskScheduler.Current);
                    ob.OnNext(dev);
                });
                this.context.Callbacks.ConnectionStateChanged += handler;
                return () => this.context.Callbacks.ConnectionStateChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.deviceStatusOb;
        }


        IObservable<IScanResult> CreateScanner(Guid? serviceUuid)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                this.context.Devices.Clear();

                var scan = this.ScanListen().Subscribe(ob.OnNext);
                this.context.StartScan(this.ForcePreLollipopScanner, serviceUuid);
                this.scanStatusChanged.OnNext(true);

                return () =>
                {
                    this.context.StopScan();
                    scan.Dispose();
                    this.scanStatusChanged.OnNext(false);
                };
            })
            .Publish()
            .RefCount();
        }
    }
}