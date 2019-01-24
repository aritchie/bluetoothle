using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    public partial class Adapter : AbstractAdapter
    {
        public static int WaitForPeripheralManagerStatusTimeOut = 3000;

        readonly AdapterContext context;

        public override string DeviceName => "Default Bluetooth Device";
        public override bool IsScanning => this.context.Manager.IsScanning;
        public virtual CBPeripheralManager PeripheralManager  => this.context?.PeripheralManager;


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


        public override IObservable<IDevice> GetKnownDevice(Guid deviceId)
        {
            var peripheral = this.context.Manager.RetrievePeripheralsWithIdentifiers(deviceId.ToNSUuid()).FirstOrDefault();
            if (peripheral == null)
                return Observable.Return<IDevice>(null);

            var device = this.context.GetDevice(peripheral);
            return Observable.Return(device);
        }


        public override IObservable<IEnumerable<IDevice>> GetPairedDevices() => Observable.Return(new IDevice[0]);

        public override IObservable<IEnumerable<IDevice>> GetConnectedDevices(Guid? serviceUuid = null)
        {
            if (serviceUuid == null)
                return Observable.Return(this.context.GetConnectedDevices().ToList());

            var list = new List<IDevice>();
            var peripherals = this.context.Manager.RetrieveConnectedPeripherals(serviceUuid.Value.ToCBUuid());
            foreach (var peripheral in peripherals)
            {
                var dev = this.context.GetDevice(peripheral);
                list.Add(dev);
            }
            return Observable.Return(list);
        }


        public override IObservable<AdapterStatus> WhenStatusChanged() => this.context
            .StateUpdated
            .Select(_ => this.Status)
            .StartWith(this.Status);


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

                return () =>
                {
                    this.context.Manager.StopScan();
                    scan.Dispose();
                };
            });
        }


        public override void StopScan() => this.context.Manager.StopScan();

        internal IObservable<CBPeripheralManagerState> GetPeripheralManagerState()
        {
            if (context.PeripheralManager == null)
                return Observable.Return(CBPeripheralManagerState.PoweredOff);

            if (context.PeripheralManager.State != CBPeripheralManagerState.Unknown)
            {
                // If we can detect state out of box just returning in
                return Observable.Return(context.PeripheralManager.State);
            }
            else
            {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                            h => context.PeripheralManager.StateUpdated += h,
                            h => context.PeripheralManager.StateUpdated -= h)
                        .Select(a => context.PeripheralManager.State);
            }
        }

        //IObservable<IDevice> deviceStatusOb;
        //public override IObservable<IDevice> WhenDeviceStatusChanged()
        //{
        //    this.deviceStatusOb = this.deviceStatusOb ??
        //        this.context
        //            .PeripheralConnected
        //            .Select(x => this.context.GetDevice(x))
        //            .Merge(this.context
        //                .PeripheralDisconnected
        //                .Select(x => this.context.GetDevice(x))
        //            )
        //            .Publish()
        //            .RefCount();

        //    return this.deviceStatusOb;
        //}


        public override IObservable<IDevice> WhenDeviceStateRestored() =>
            this.context
                .WhenWillRestoreState
                .AsObservable();
	}
}