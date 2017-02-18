using System;
using Android.Bluetooth;

namespace Plugin.BluetoothLE.Internals
{
    public class PreLollipopScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
    {
        readonly Action<ScanEventArgs> callback;


        public PreLollipopScanCallback(Action<ScanEventArgs> callback)
        {
            this.callback = callback;
        }


        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            this.callback(new ScanEventArgs(device, rssi, scanRecord));
        }
    }
}