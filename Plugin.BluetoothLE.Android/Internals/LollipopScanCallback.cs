using System;
using Android.Bluetooth.LE;
using SR = Android.Bluetooth.LE.ScanResult;


namespace Plugin.BluetoothLE.Internals
{
    public class LollipopScanCallback : ScanCallback
    {
        readonly Action<ScanEventArgs> callback;


        public LollipopScanCallback(Action<ScanEventArgs> callback)
        {
            this.callback = callback;
        }


        public override void OnScanResult(ScanCallbackType callbackType, SR result)
        {
            this.callback(new ScanEventArgs(result.Device, result.Rssi, result.ScanRecord));
        }
    }
}