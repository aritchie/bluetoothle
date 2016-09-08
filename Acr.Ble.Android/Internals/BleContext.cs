using System;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;


namespace Acr.Ble.Internals
{
    public class BleContext
    {
        public GattCallbacks Callbacks { get; }
        readonly BluetoothManager manager;
        LollipopScanCallback newCallback;
        PreLollipopScanCallback oldCallback;


        public BleContext(BluetoothManager manager)
        {
            this.manager = manager;
            this.Callbacks = new GattCallbacks();
            this.Devices = new DeviceManager(manager, this.Callbacks);
        }


        public DeviceManager Devices { get; }

        public void StartScan(bool forcePreLollipop, ScanFilter scanFilter, Action<ScanEventArgs> callback)
        {
            this.Devices.Clear();
            if (!forcePreLollipop && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.newCallback = new LollipopScanCallback(callback);
                var builder = new ScanSettings.Builder();
                this.manager.Adapter.BluetoothLeScanner.StartScan(null, builder.Build(), this.newCallback);
            }
            else
            {
                this.oldCallback = new PreLollipopScanCallback(callback);
                // first arg takes UUID[] args
                this.manager.Adapter.StartLeScan(this.oldCallback);
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
    }
}