using System;
using Android.Bluetooth;
using Android.Bluetooth.LE;
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
    }
}