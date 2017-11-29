using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using ScanMode = Android.Bluetooth.LE.ScanMode;


namespace Plugin.BluetoothLE.Internals
{
    public class AdapterContext
    {
        public GattCallbacks Callbacks { get; } = new GattCallbacks();


        readonly BluetoothManager manager;
        LollipopScanCallback newCallback;
        PreLollipopScanCallback oldCallback;


        public AdapterContext(BluetoothManager manager)
        {
            this.manager = manager;
            this.Devices = new DeviceManager(manager, this.Callbacks);
        }


        public DeviceManager Devices { get; }
        public event EventHandler<ScanEventArgs> Scanned;


        public void StartScan(ScanConfig config)
        {
            this.Devices.Clear();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.StartNewScanner(config);
            }
            else
            {
                this.StartPreLollipopScan();
            }
        }


        public void StopScan()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.manager.Adapter.BluetoothLeScanner?.StopScan(this.newCallback);
            }
            else
            {
                this.manager.Adapter.StopLeScan(this.oldCallback);
            }
        }


        protected virtual void StartNewScanner(ScanConfig config)
        {
            this.newCallback = new LollipopScanCallback(args => this.Scanned?.Invoke(this, args));
            var scanMode = this.ToNative(config.ScanType);
            var filterBuilderList = new List<ScanFilter>();
            if (config.ServiceUuids != null && config.ServiceUuids.Count > 0)
            {
                foreach (var uuid in config.ServiceUuids)
                {
                    var filterBuilder = new ScanFilter.Builder();
                    filterBuilder.SetServiceUuid(uuid.ToParcelUuid());
                    filterBuilderList.Add(filterBuilder.Build());
                }
            }
            else
            {
                var filterBuilder = new ScanFilter.Builder();
                filterBuilderList.Add(filterBuilder.Build());
            }
            //new ScanFilter.Builder().SetDeviceAddress().Set
            this.manager.Adapter.BluetoothLeScanner.StartScan(
                filterBuilderList,
                new ScanSettings
                    .Builder()
                    .SetScanMode(scanMode)
                    .Build(),
                this.newCallback
            );
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


        // TODO: scanfilter?
        protected virtual void StartPreLollipopScan()
        {
            this.oldCallback = new PreLollipopScanCallback(args => this.Scanned?.Invoke(this, args));
            this.manager.Adapter.StartLeScan(this.oldCallback);
        }
    }
}
