using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using ScanMode = Android.Bluetooth.LE.ScanMode;


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

        public void StartScan(bool forcePreLollipop, Guid? bgServiceUuid, Action<ScanEventArgs> callback)
        {
            this.Devices.Clear();
            if (!forcePreLollipop && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.newCallback = new LollipopScanCallback(callback);

                if (bgServiceUuid == null)
                {
                    this.manager.Adapter.BluetoothLeScanner.StartScan(
                        null,
                        new ScanSettings
                            .Builder()
                            .SetScanMode(ScanMode.Balanced)
                            .Build(),
                        this.newCallback
                    );
                }
                else
                {
                    this.manager.Adapter.BluetoothLeScanner.StartScan(
                        new List<ScanFilter>
                        {
                            new ScanFilter
                                .Builder()
                                .SetServiceUuid(bgServiceUuid.Value.ToParcelUuid())
                                .Build()
                        },
                        new ScanSettings
                            .Builder()
                            .SetScanMode(ScanMode.LowPower)
                            .Build(),
                        this.newCallback
                    );
                }
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