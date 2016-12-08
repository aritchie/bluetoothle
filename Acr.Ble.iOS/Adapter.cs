using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreBluetooth;
using CoreFoundation;
using System.Reactive.Subjects;
using UIKit;
using Foundation;
using ObjCRuntime;

namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly DeviceManager deviceManager;
        readonly CBCentralManager manager;
        readonly Subject<bool> scanStatusChanged;


        public Adapter()
        {
            this.manager = new CBCentralManager(DispatchQueue.DefaultGlobalQueue);
            //this.manager = new CBCentralManager(DispatchQueue.GetGlobalQueue(DispatchQueuePriority.Background));
            this.deviceManager = new DeviceManager(this.manager);
            this.scanStatusChanged = new Subject<bool>();
        }


        public AdapterStatus Status
        {
            get
            {
                switch (this.manager.State)
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
            return this.deviceManager.GetConnectedDevices();
        }


        IObservable<AdapterStatus> statusOb;
        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new EventHandler((sender, args) => ob.OnNext(this.Status));
                this.manager.UpdatedState += handler;

                return () => this.manager.UpdatedState -= handler;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


#if __IOS__ || __TVOS__
        public bool IsScanning => this.manager.IsScanning;
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


        IObservable<IScanResult> scanner;
        public IObservable<IScanResult> Scan()
        {
            this.scanner = this.scanner ?? this.CreateScanner();
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
                var handler = new EventHandler<CBDiscoveredPeripheralEventArgs>((sender, args) =>
                {
                    var device = this.deviceManager.GetDevice(args.Peripheral);
                    ob.OnNext(new ScanResult(
                        device,
                        args.RSSI?.Int32Value ?? 0,
                        new AdvertisementData(args.AdvertisementData))
                    );
                });
                this.manager.DiscoveredPeripheral += handler;
                return () => this.manager.DiscoveredPeripheral -= handler;;
            });
            return this.scanListenOb;
        }


        IObservable<IDevice> deviceStatusOb;
        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            this.deviceStatusOb = this.deviceStatusOb ?? Observable.Create<IDevice>(observer =>
            {
                var chandler = new EventHandler<CBPeripheralEventArgs>((sender, args) =>
                {
                    var device = this.deviceManager.GetDevice(args.Peripheral);
                    observer.OnNext(device);
                });
                var dhandler = new EventHandler<CBPeripheralErrorEventArgs>((sender, args) =>
                {
                    var device = this.deviceManager.GetDevice(args.Peripheral);
                    observer.OnNext(device);
                });

                this.manager.ConnectedPeripheral += chandler;
                this.manager.DisconnectedPeripheral += dhandler;

                return () =>
                {
                    this.manager.ConnectedPeripheral -= chandler;
                    this.manager.DisconnectedPeripheral -= dhandler;
                };
            });
            return this.deviceStatusOb;
        }


        public bool OpenSettings()
        {
            var flag = false;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                var workSpaceClassId = Class.GetHandle("LSApplicationWorkspace");
                if (workSpaceClassId != IntPtr.Zero) 
                {
                    var workSpaceClass = new NSObject(workSpaceClassId);
                    var workSpaceInstance = workSpaceClass.PerformSelector(new Selector("defaultWorkspace"));

                    var selector = new Selector("openSensitiveURL:withOptions:");
                    if (workSpaceInstance.RespondsToSelector(selector))
                    {
                        workSpaceInstance.PerformSelector(selector, new NSUrl("Prefs:root=Bluetooth"), null);
                        flag = true;
                    }
                }
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                flag = OpenUrl("prefs:root=Bluetooth");
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                flag = OpenUrl("prefs:root=General&path=Bluetooth");
            }
            return flag;
        }


        static bool OpenUrl(string url)
        {
            var nsurl = new NSUrl(url);
            if (!UIApplication.SharedApplication.CanOpenUrl(nsurl))
                return false;

            UIApplication.SharedApplication.OpenUrl(nsurl);
            return true;            
        }


        IObservable<IScanResult> CreateScanner(Guid? serviceUuid = null)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                this.deviceManager.Clear();
                var scan = this.ScanListen().Subscribe(ob.OnNext);

                if (serviceUuid == null)
                {
                    this.manager.ScanForPeripherals(null, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                }
                else
                {
                    var uuid = serviceUuid.Value.ToCBUuid();
                    this.manager.ScanForPeripherals(uuid);
                }
                this.ToggleScanStatus(true);

                return () =>
                {
                    this.manager.StopScan();
                    scan.Dispose();
                    this.ToggleScanStatus(false);
                };
            })
            .Publish()
            .RefCount();
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