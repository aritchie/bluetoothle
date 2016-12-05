using System;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using ScanMode = Android.Bluetooth.LE.ScanMode;


namespace Acr.Ble.Internals
{
    public class BleContext
    {
        public GattCallbacks Callbacks { get; } = new GattCallbacks();
        public object ReadWriteLock { get; } = new object();
        readonly BluetoothManager manager;
        LollipopScanCallback newCallback;
        PreLollipopScanCallback oldCallback;


        public BleContext(BluetoothManager manager)
        {
            this.manager = manager;
            this.Devices = new DeviceManager(manager, this.Callbacks);
        }


        public DeviceManager Devices { get; }
        public event EventHandler<ScanEventArgs> Scanned;


        public void StartScan(bool forcePreLollipop, bool bgScan)
        {
            this.Devices.Clear();
            if (!forcePreLollipop && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.StartNewScanner(bgScan);
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
                this.manager.Adapter.BluetoothLeScanner.StopScan(this.newCallback);
            }
            else
            {
                this.manager.Adapter.StopLeScan(this.oldCallback);
            }
        }


        protected virtual void StartNewScanner(bool bgScan)
        {
            this.newCallback = new LollipopScanCallback(args => this.Scanned?.Invoke(this, args));
            var scanMode = bgScan ? ScanMode.LowPower : ScanMode.Balanced;
            this.manager.Adapter.BluetoothLeScanner.StartScan(
                null,
                new ScanSettings
                    .Builder()
                    .SetScanMode(scanMode)
                    .Build(),
                this.newCallback
            );
        }


        protected virtual void StartPreLollipopScan()
        {
            this.oldCallback = new PreLollipopScanCallback(args => this.Scanned?.Invoke(this, args));
            this.manager.Adapter.StartLeScan(this.oldCallback);
        }
    }
}