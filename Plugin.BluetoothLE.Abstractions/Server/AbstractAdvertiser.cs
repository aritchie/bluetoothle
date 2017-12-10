using System;


namespace Plugin.BluetoothLE.Server
{
    public abstract class AbstractAdvertiser : IAdvertiser
    {
        public bool IsStarted { get; protected set; }
        public AdvertisementData CurrentAdvertisementData { get; protected set; }


        public virtual void Start(AdvertisementData adData)
        {
            this.CurrentAdvertisementData = adData;
            throw new NotImplementedException();
        }


        public virtual void Stop()
        {
            this.CurrentAdvertisementData = null;
            this.IsStarted = false;
            throw new NotImplementedException();
        }
    }
}
