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
        public event EventHandler<ScanEventArgs> Scanned;


        public void StartScan(bool forcePreLollipop, Guid? bgServiceUuid)
        {
            this.Devices.Clear();
            if (!forcePreLollipop && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                this.StartNewScanner(bgServiceUuid);
            }
            else
            {
                this.StartPreLollipopScan(bgServiceUuid);
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


        protected virtual void StartNewScanner(Guid? bgServiceUuid)
        {
            this.newCallback = new LollipopScanCallback(args => this.Scanned?.Invoke(this, args));

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


        protected virtual void StartPreLollipopScan(Guid? bgServiceUuid)
        {
            this.oldCallback = new PreLollipopScanCallback(args => this.Scanned?.Invoke(this, args));
            if (bgServiceUuid == null)
            {
                this.manager.Adapter.StartLeScan(this.oldCallback);
            }
            else
            {
                this.manager.Adapter.StartLeScan(
                    new[] { bgServiceUuid.Value.ToUuid() },
                    this.oldCallback
                );
            }
        }
    }
}