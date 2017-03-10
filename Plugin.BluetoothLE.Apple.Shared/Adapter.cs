using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
#if __IOS__
using UIKit;
using Foundation;
using ObjCRuntime;
#endif


namespace Plugin.BluetoothLE
{
    public class Adapter : IAdapter
    {
        readonly BleContext context;
        readonly Subject<bool> scanStatusChanged;


        public Adapter(BleAdapterConfiguration config = null)
        {
            this.context = new BleContext(config);
            this.scanStatusChanged = new Subject<bool>();
        }

#if __IOS__
        public AdapterFeatures Features
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
        public AdapterFeatures Features => AdapterFeatures.None;
#endif


        public AdapterStatus Status
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


        public IDevice GetKnownDevice(Guid deviceId)
        {
            var peripheral = this.context.Manager.RetrievePeripheralsWithIdentifiers(deviceId.ToNSUuid()).FirstOrDefault();
            var device = this.context.GetDevice(peripheral);
            return device;
        }


        public IEnumerable<IDevice> GetPairedDevices()
        {
            return new IDevice[0];
        }


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.context.GetConnectedDevices();
        }


        IObservable<AdapterStatus> statusOb;
        public IObservable<AdapterStatus> WhenStatusChanged()
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
        public bool IsScanning => this.context.Manager.IsScanning;
#else
        public bool IsScanning { get; private set; }
#endif


        public IObservable<bool> WhenScanningStatusChanged()
        {
            return this.scanStatusChanged
                .AsObservable()
                .StartWith(this.IsScanning);
        }


        public IObservable<IScanResult> Scan(ScanConfig config)
        {
            config = config ?? new ScanConfig();

            if (this.Status != AdapterStatus.PoweredOn)
                throw new ArgumentException("Your adapter status is " + this.Status);

            if (this.IsScanning)
                throw new ArgumentException("There is already an existing scan");

            if (config.ScanType == BleScanType.Background && config.ServiceUuid == null)
                throw new ArgumentException("Background scan type set but not ServiceUUID");

            return Observable.Create<IScanResult>(ob =>
            {
                this.context.Clear();
                var scan = this.ScanListen().Subscribe(ob.OnNext);

                if (config.ServiceUuid == null)
                {
                    this.context.Manager.ScanForPeripherals(null, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                }
                else
                {
                    var uuid = config.ServiceUuid.Value.ToCBUuid();
                    this.context.Manager.ScanForPeripherals(uuid);
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


        IObservable<IScanResult> scanListenOb;
        public IObservable<IScanResult> ScanListen()
        {
            this.scanListenOb = this.scanListenOb ?? this.context.ScanResultReceived.AsObservable();
            return this.scanListenOb;
        }


        IObservable<IDevice> deviceStatusOb;
        public IObservable<IDevice> WhenDeviceStatusChanged()
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


        public bool EnableAdapterState(bool enabled)
        {
            return false;
        }


#if __IOS__

        public void OpenSettings()
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

#else
        public void OpenSettings()
        {
        }
#endif


        public void SetAdapterState(bool enabled)
        {
        }


        public IObservable<IDevice> WhenDeviceStateRestored()
        {
            return this.context
                .WhenWillRestoreState
                .AsObservable();
        }


        void ToggleScanStatus(bool isScanning)
        {
            this.scanStatusChanged.OnNext(isScanning);
#if MAC
            this.IsScanning = isScanning;
#endif
        }
	}
}