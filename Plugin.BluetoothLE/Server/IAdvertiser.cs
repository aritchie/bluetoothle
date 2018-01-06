using System;


namespace Plugin.BluetoothLE.Server
{
    public interface IAdvertiser
    {
        bool IsStarted { get; }
        AdvertisementData CurrentAdvertisementData { get; }

        void Start(AdvertisementData adData);
        void Stop();
    }
}
