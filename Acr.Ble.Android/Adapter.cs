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


        public IObservable<bool> WhenScanningStatusChanged()
        {
            return this.scanStatusChanged;
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


        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            return Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var aob = BluetoothObservables
                    .WhenAdapterStatusChanged()
                    .Subscribe(_ => ob.OnNext(this.Status));

                return aob.Dispose;
            });
        }


        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            return Observable.Create<IDevice>(ob =>
            {
                var handler = new EventHandler<ConnectionStateEventArgs>((sender, args) =>
                {
                    var dev = this.context.Devices.GetDevice(args.Gatt.Device, TaskScheduler.Current);
                    ob.OnNext(dev);
                });
                this.context.Callbacks.ConnectionStateChanged += handler;
                return () => this.context.Callbacks.ConnectionStateChanged -= handler;
            });
        }


        IObservable<IScanResult> CreateScanner(Guid? serviceUuid)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                this.context.StartScan(this.ForcePreLollipopScanner, serviceUuid, x =>
                {
                    var dev = this.context.Devices.GetDevice(x.Device, TaskScheduler.Current);
                    ob.OnNext(new ScanResult(dev, x.Rssi, x.AdvertisementData));
                });
                this.scanStatusChanged.OnNext(true);
                return () =>
                {
                    this.context.StopScan();
                    this.scanStatusChanged.OnNext(false);
                };
            })
            .Publish()
            .RefCount();
        }
    }
}