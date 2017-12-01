using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Plugin.BluetoothLE.Internals;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly BluetoothManager manager;
        readonly AdapterContext context;
        readonly Subject<bool> scanStatusChanged;


        public Adapter()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Application.BluetoothService);
            this.context = new AdapterContext(this.manager);
            this.scanStatusChanged = new Subject<bool>();
        }


        public override string DeviceName => "Default Bluetooth Device";
        public override AdapterFeatures Features => AdapterFeatures.All;
        public override bool IsScanning => this.manager.Adapter.IsDiscovering;
        public override IGattServer CreateGattServer() => new GattServer();


        public override IDevice GetKnownDevice(Guid deviceId)
        {
            var native = this.manager.Adapter.GetRemoteDevice(deviceId
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray()
            );
            var device = this.context.Devices.GetDevice(native);
            return device;
        }


        public override IEnumerable<IDevice> GetPairedDevices() =>
            this.manager
                .Adapter
                .BondedDevices
                .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le) // TODO: does it know?
                .Select(this.context.Devices.GetDevice)
                .ToList();


        public override IEnumerable<IDevice> GetConnectedDevices() =>
            this.manager
                .GetConnectedDevices(ProfileType.Gatt)
                .Select(this.context.Devices.GetDevice);


        public override AdapterStatus Status
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
        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? BluetoothObservables
                .WhenAdapterStatusChanged()
                .StartWith(this.Status)
                .Select(x => this.Status)
                .Replay(1)
                .RefCount();

            return this.statusOb;
        }


        public override IObservable<bool> WhenScanningStatusChanged() =>
            this.scanStatusChanged
                .AsObservable()
                .StartWith(this.IsScanning);


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            config = config ?? new ScanConfig();
            return this.context.Scan(config);
        }


        IObservable<IDevice> deviceStatusOb;
        public override IObservable<IDevice> WhenDeviceStatusChanged()
        {
            this.deviceStatusOb = this.deviceStatusOb ?? this.context
                .Callbacks
                .ConnectionStateChanged
                .Select(args => this.context.Devices.GetDevice(args.Gatt.Device))
                .Publish()
                .RefCount();

            return this.deviceStatusOb;
        }


        public override void OpenSettings()
        {
            var intent = new Intent(Android.Provider.Settings.ActionBluetoothSettings);
            intent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
        }


        public override void SetAdapterState(bool enable)
        {
            if (enable && !BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Enable();

            else if (!enable && BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Disable();
        }
    }
}