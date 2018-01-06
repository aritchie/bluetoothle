using System;


namespace Plugin.BluetoothLE
{
    public class ScanResult : IScanResult
    {
        public ScanResult(IDevice device, int rssi, IAdvertisementData adData)
        {
            this.Device = device;
            this.Rssi = rssi;
            this.AdvertisementData = adData;
        }


        public IDevice Device { get; }
        public int Rssi { get; }
        public IAdvertisementData AdvertisementData { get; }
    }
}
