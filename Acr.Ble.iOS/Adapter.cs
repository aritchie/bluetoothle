using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
#if __IOS__
using UIKit;
using Foundation;
using ObjCRuntime;
#endif


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly BleContext context = new BleContext();
        readonly Subject<bool> scanStatusChanged = new Subject<bool>();


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
                throw new ArgumentException("There is already an existing scan");

            if (config.ScanType == BleScanType.Background && config.ServiceUuid == null)
                throw new ArgumentException("Background scan type set but not ServiceUUID");

            config = config ?? new ScanConfig();
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

        //public bool CanOpenSettings => !UIDevice.CurrentDevice.CheckSystemVersion(10, 0); // if it is 8 or 9 but not 10
        public bool CanOpenSettings => UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);


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
        public bool CanOpenSettings => false;


        public void OpenSettings()
        {
        }
#endif


        public bool CanChangeAdapterState => false;
        public void SetAdapterState(bool enabled)
        {
        }


        protected virtual void OnWillRestoreState(object sender, CBWillRestoreEventArgs args)
        {
            // TODO: rehydrate device manager
            // TODO: do I have to set services and characteristics again?
            // TODO: if I was connecting, do I want to trigger a connection again?
        }

        /*
http://stackoverflow.com/questions/22412376/corebluetooth-state-preservation-issue-willrestorestate-not-called-in-ios-7-1


State Preservation and Restoration
Because state preservation and restoration is built in to Core Bluetooth, your app can opt in to this feature to ask the system to preserve the state of your app’s central and peripheral managers and to continue performing certain Bluetooth-related tasks on their behalf, even when your app is no longer running. When one of these tasks completes, the system relaunches your app into the background and gives your app the opportunity to restore its state and to handle the event appropriately. In the case of the home security app described above, the system would monitor the connection request, and re-relaunch the app to handle the centralManager:didConnectPeripheral: delegate callback when the user returned home and the connection request completed.

Core Bluetooth supports state preservation and restoration for apps that implement the central role, peripheral role, or both. When your app implements the central role and adds support for state preservation and restoration, the system saves the state of your central manager object when the system is about to terminate your app to free up memory (if your app has multiple central managers, you can choose which ones you want the system to keep track of). In particular, for a given CBCentralManager object, the system keeps track of:

The services the central manager was scanning for (and any scan options specified when the scan started)
The peripherals the central manager was trying to connect to or had already connected to
The characteristics the central manager was subscribed to
Apps that implement the peripheral role can likewise take advantage of state preservation and restoration. For CBPeripheralManager objects, the system keeps track of:

The data the peripheral manager was advertising
The services and characteristics the peripheral manager published to the device’s database
The centrals that were subscribed to your characteristics’ values
When your app is relaunched into the background by the system (because a peripheral your app was scanning for is discovered, for instance), you can reinstantiate your app’s central and peripheral managers and restore their state. The following section describes in detail how to take advantage of state preservation and restoration in your app.

Adding Support for State Preservation and Restoration
State preservation and restoration in Core Bluetooth is an opt-in feature and requires help from your app to work. You can add support for this feature in your app by following this process:

(Required) Opt in to state preservation and restoration when you allocate and initialize a central or peripheral manager object. This step is described in Opt In to State Preservation and Restoration.
(Required) Reinstantiate any central or peripheral manager objects after your app is relaunched by the system. This step is described in Reinstantiate Your Central and Peripheral Managers.
(Required) Implement the appropriate restoration delegate method. This step is described in Implement the Appropriate Restoration Delegate Method.
(Optional) Update your central and peripheral managers’ initialization process. This step is described in Update Your Initialization Process.

         */


        void ToggleScanStatus(bool isScanning)
        {
            this.scanStatusChanged.OnNext(isScanning);
#if MAC
            this.IsScanning = isScanning;
#endif
        }
    }
}