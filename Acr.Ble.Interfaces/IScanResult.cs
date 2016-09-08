using System;


namespace Acr.Ble
{
    public interface IScanResult
    {
        int Rssi { get; }
        IDevice Device { get; }
        IAdvertisementData AdvertisementData { get; }
    }
}
