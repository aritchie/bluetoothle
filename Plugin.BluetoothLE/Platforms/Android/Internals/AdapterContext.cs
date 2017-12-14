using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using ScanMode = Android.Bluetooth.LE.ScanMode;


namespace Plugin.BluetoothLE.Internals
{
    public class AdapterContext
    {
        readonly BluetoothManager manager;


        public AdapterContext(BluetoothManager manager)
        {
            this.manager = manager;
            this.Callbacks = new GattCallbacks();
            this.Devices = new DeviceManager(manager, this.Callbacks);
        }


        public GattCallbacks Callbacks { get; }
        public DeviceManager Devices { get; }


        public IObservable<ScanResult> Scan(ScanConfig config)
        {
            this.Devices.Clear();
            var obs = CrossBleAdapter.AndroidUseNewScanner
                ? this.NewScan(config)
                : this.PreLollipopScan(config);

            return obs;
        }


        protected virtual IObservable<ScanResult> NewScan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
        {
            var cb = new LollipopScanCallback((native, rssi, sr) =>
            {
                var scanResult = this.ToScanResult(native, rssi, new AdvertisementData(sr));
                ob.OnNext(scanResult);
            });
            var scanMode = this.ToNative(config.ScanType);

            var scanFilters = new List<ScanFilter>();
            if (config.ServiceUuids != null && config.ServiceUuids.Count > 0)
            {
                foreach (var guid in config.ServiceUuids)
                {
                    var uuid = guid.ToParcelUuid();
                    scanFilters.Add(new ScanFilter.Builder()
                        .SetServiceUuid(uuid)
                        .Build()
                    );
                }
            }

            this.manager.Adapter.BluetoothLeScanner.StartScan(
                scanFilters,
                new ScanSettings
                    .Builder()
                    .SetScanMode(scanMode)
                    .Build(),
                cb
            );

            return () => this.manager.Adapter.BluetoothLeScanner?.StopScan(cb);
        });


        protected virtual IObservable<ScanResult> PreLollipopScan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
        {
            var cb = new PreLollipopScanCallback((native, rssi, sr) =>
            {
                var ad = new AdvertisementData(sr);
                if (this.IsFiltered(ad, config))
                {
                    var scanResult = this.ToScanResult(native, rssi, ad);
                    ob.OnNext(scanResult);
                }
            });
            this.manager.Adapter.StartLeScan(cb);

            return () => this.manager.Adapter.StopLeScan(cb);
        });


        protected bool IsFiltered(AdvertisementData ad, ScanConfig config)
        {
            if (config.ServiceUuids == null ||
                !config.ServiceUuids.Any() ||
                ad.ServiceUuids == null ||
                !ad.ServiceUuids.Any())
                return true;

            foreach (var adUuid in ad.ServiceUuids)
            {
                foreach (var uuid in config.ServiceUuids)
                {
                    if (uuid.Equals(adUuid))
                        return true;
                }
            }

            return false;
        }

        protected ScanResult ToScanResult(BluetoothDevice native, int rssi, IAdvertisementData ad)
        {
            var dev = this.Devices.GetDevice(native);
            var result = new ScanResult(dev, rssi, ad);
            return result;
        }


        protected virtual ScanMode ToNative(BleScanType scanType)
        {
            switch (scanType)
            {
                case BleScanType.Background:
                case BleScanType.LowPowered:
                    return ScanMode.LowPower;

                case BleScanType.Balanced:
                    return ScanMode.Balanced;

                case BleScanType.LowLatency:
                    return ScanMode.LowLatency;

                default:
                    throw new ArgumentException("Invalid BleScanType");
            }
        }
    }
}