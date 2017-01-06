using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Acr.Ble.Internals;
using Android.App;
using Android.Bluetooth;
using Android.Content;
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


        public bool IsScanning => this.manager.Adapter.IsDiscovering;


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.context.Devices.GetConnectedDevices();
        }


        public AdapterStatus Status
        {
            get
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2)
                    return AdapterStatus.Unsupported;

                //if (!Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                //    return AdapterStatus.Unsupported;

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
            this.statusOb = this.statusOb ?? BluetoothObservables
                .WhenAdapterStatusChanged()
                .StartWith(this.Status)
                .Select(x => this.Status)
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


        public IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            config = config ?? new ScanConfig();
            return Observable.Create<IScanResult>(ob =>
            {
                this.context.Devices.Clear();

                var scan = this.ScanListen().Subscribe(ob.OnNext);
                this.context.StartScan(AndroidConfig.ForcePreLollipopScanner, config);
                this.scanStatusChanged.OnNext(true);

                return () =>
                {
                    this.context.StopScan();
                    scan.Dispose();
                    this.scanStatusChanged.OnNext(false);
                };
            });
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


        public bool CanOpenSettings => true;


        public void OpenSettings()
        {
            var intent = new Intent(Android.Provider.Settings.ActionBluetoothSettings);
            intent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
        }


        public bool CanChangeAdapterState => true;

        public void SetAdapterState(bool enable)
        {
            if (enable && !BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Enable();

            else if (!enable && BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Disable();
        }


        public IObservable<IDevice> WhenDeviceStateRestored()
        {
            return Observable.Empty<IDevice>();
        }
    }
}