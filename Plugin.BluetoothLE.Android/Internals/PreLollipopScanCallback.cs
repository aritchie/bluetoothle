using System;
using Android.Bluetooth;

namespace Plugin.BluetoothLE.Internals
{
    public class PreLollipopScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
    {
        readonly Action<BluetoothDevice, int, byte[]> callback;


        public PreLollipopScanCallback(Action<BluetoothDevice, int, byte[]> callback)
            => this.callback = callback;


        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
            => this.callback(device, rssi, scanRecord);
    }
}