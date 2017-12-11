using System;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class ScanResult : IScanResult
    {
        public ScanResult(BluetoothLeDevice native, IDevice device)
        {
            this.Device = device;
            this.Rssi = native.Rssi;
            this.AdvertisementData = new AdvertisementData(native);
        }


        public int Rssi { get; }
        public IDevice Device { get; }
        public IAdvertisementData AdvertisementData { get; }
    }
}
