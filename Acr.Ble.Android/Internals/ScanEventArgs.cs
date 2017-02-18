using System;
using Android.Bluetooth;
using Android.Bluetooth.LE;


namespace Plugin.BluetoothLE.Internals {

    public class ScanEventArgs : EventArgs
    {
        public ScanEventArgs(BluetoothDevice device, int rssi, ScanRecord scanRecord) : this(device, rssi)
        {
            this.AdvertisementData = new AdvertisementData(scanRecord);
        }


        public ScanEventArgs(BluetoothDevice device, int rssi, byte[] advertisementData) : this(device, rssi)
        {
            this.AdvertisementData = new AdvertisementData(advertisementData);
        }


        ScanEventArgs(BluetoothDevice device, int rssi)
        {
            this.Device = device;
            this.Rssi = rssi;
        }


        public AdvertisementData AdvertisementData { get; }
        public int Rssi { get; }
        public BluetoothDevice Device { get; }
    }
}