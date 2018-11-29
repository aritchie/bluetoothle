using System;
using System.Collections.Generic;
using Acr.Logging;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using SR = Android.Bluetooth.LE.ScanResult;


namespace Plugin.BluetoothLE.Internals
{
    public class LollipopScanCallback : ScanCallback
    {
        readonly Action<BluetoothDevice, int, ScanRecord> callback;


        public LollipopScanCallback(Action<BluetoothDevice, int, ScanRecord> callback)
            => this.callback = callback;


        public override void OnScanResult(ScanCallbackType callbackType, SR result)
            => this.callback(result.Device, result.Rssi, result.ScanRecord);

        public override void OnBatchScanResults(IList<SR> results)
        {
            if (results == null) return;
            foreach (SR result in results) this.callback(result.Device, result.Rssi, result.ScanRecord);
        }

        public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
        {
            Log.Error(BleLogCategory.Adapter, $"Scan failed: {errorCode}");
            base.OnScanFailed(errorCode);
        }
    }
}
