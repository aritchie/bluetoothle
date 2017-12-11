using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Plugin.BluetoothLE.Server;
#if __IOS__
using UIKit;
using Foundation;
using ObjCRuntime;
#endif


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly AdapterContext context;
        readonly Subject<bool> scanStatusChanged;


        public Adapter(BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(config);
            this.scanStatusChanged = new Subject<bool>();
        }


        public override string DeviceName => "Default Bluetooth Device";
#if __IOS__
        public override AdapterFeatures Features
        {
            get
            {
                var v8or9 = UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);
                return v8or9
                    ? AdapterFeatures.OpenSettings
                    : AdapterFeatures.None;
            }
        }
#else
        public override AdapterFeatures Features => AdapterFeatures.None;
#endif


        public override IGattServer CreateGattServer() => new GattServer();


        public override AdapterStatus Status
        {
            get
            {
                switch (this.context.Manager.State)
                {
                    case CBCentralManagerState.PoweredOff:
                        return AdapterStatus.PoweredOff;

                    case CBCentralManagerState.PoweredOn:
                        return AdapterStatus.PoweredOn;

                    case CBCentralManagerState.Resetting:
                        return AdapterStatus.Resetting;

                    case CBCentralManagerState.Unauthorized:
                        return AdapterStatus.Unauthorized;

                    case CBCentralManagerState.Unsupported:
                        return AdapterStatus.Unsupported;

                    case CBCentralManagerState.Unknown:
                    default:
                        return AdapterStatus.Unknown;
                }
            }
        }


        public override IDevice GetKnownDevice(Guid deviceId)
        {
            var peripheral = this.context.Manager.RetrievePeripheralsWithIdentifiers(deviceId.ToNSUuid()).FirstOrDefault();
            var device = this.context.GetDevice(peripheral);
            return device;
        }


        public override IEnumerable<IDevice> GetPairedDevices() => new IDevice[0];
        public override IEnumerable<IDevice> GetConnectedDevices() => this.context.GetConnectedDevices();


        IObservable<AdapterStatus> statusOb;
        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? this.context
                .StateUpdated
                .StartWith(this.Status)
                .Select(_ => this.Status)
                .Replay(1)
                .RefCount();

            return this.statusOb;
        }


#if __IOS__ || __TVOS__
        public override bool IsScanning => this.context.Manager.IsScanning;
#endif


        public override IObservable<bool> WhenScanningStatusChanged() =>
            this.scanStatusChanged
                .AsObservable()
                .StartWith(this.IsScanning);


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            config = config ?? new ScanConfig();

            if (this.Status != AdapterStatus.PoweredOn)
                throw new ArgumentException("Your adapter status is " + this.Status);

            if (this.IsScanning)
                throw new ArgumentException("There is already an existing scan");

            if (config.ScanType == BleScanType.Background && (config.ServiceUuids == null || config.ServiceUuids.Count == 0))
                throw new ArgumentException("Background scan type set but not ServiceUUID");

            return Observable.Create<IScanResult>(ob =>
            {
                this.context.Clear();
                var scan = this.context
                    .ScanResultReceived
                    .AsObservable()
                    .Subscribe(ob.OnNext);

                if (config.ServiceUuids == null || config.ServiceUuids.Count == 0)
                {
                    this.context.Manager.ScanForPeripherals(null, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                }
                else
                {
                    var uuids = config.ServiceUuids.Select(o => o.ToCBUuid()).ToArray();
                    if (config.ScanType == BleScanType.Background)
                    {
                        this.context.Manager.ScanForPeripherals(uuids);
                    }
                    else
                    {
                        this.context.Manager.ScanForPeripherals(uuids, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                    }
                }
                this.ToggleScanStatus(true);

                return () =>
                {
                    this.context.Manager.StopScan();
                    scan.Dispose();
                    this.ToggleScanStatus(false);
                };
            });
        }


        IObservable<IDevice> deviceStatusOb;
        public override IObservable<IDevice> WhenDeviceStatusChanged()
        {
            this.deviceStatusOb = this.deviceStatusOb ?? Observable.Merge(
                this.context
                    .PeripheralConnected
                    .Select(x => this.context.GetDevice(x)),

                this.context
                    .PeripheralDisconnected
                    .Select(x => this.context.GetDevice(x))
            )
            .Publish()
            .RefCount();

            return this.deviceStatusOb;
        }

#if __IOS__

        public override void OpenSettings()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                //var workSpaceClassId = Class.GetHandle("LSApplicationWorkspace");
                //if (workSpaceClassId != IntPtr.Zero)
                //{
                //    var workSpaceClass = new NSObject(workSpaceClassId);
                //    var workSpaceInstance = workSpaceClass.PerformSelector(new Selector("defaultWorkspace"));

                //    var selector = new Selector("openSensitiveURL:withOptions:");
                //    if (workSpaceInstance.RespondsToSelector(selector))
                //    {
                //        workSpaceInstance.PerformSelector(selector, new NSUrl("Prefs:root=Bluetooth"));
                //    }
                //}
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                OpenUrl("prefs:root=Bluetooth");
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                OpenUrl("prefs:root=General&path=Bluetooth");
            }
        }


        static void OpenUrl(string url)
        {
            var nsurl = new NSUrl(url);
            if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                UIApplication.SharedApplication.OpenUrl(nsurl);
        }

#endif

        public override IObservable<IDevice> WhenDeviceStateRestored() =>
            this.context
                .WhenWillRestoreState
                .AsObservable();


        void ToggleScanStatus(bool isScanning)
        {
            this.scanStatusChanged.OnNext(isScanning);
#if MAC
            this.IsScanning = isScanning;
#endif
        }
	}
}