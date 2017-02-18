using System;


namespace Plugin.BluetoothLE
{
    public interface IScanResult
    {
        int Rssi { get; }
        IDevice Device { get; }
        IAdvertisementData AdvertisementData { get; }
    }
}
