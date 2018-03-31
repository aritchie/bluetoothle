#if __IOS__ || __MACOS__
using System;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly CBPeripheralManager manager = new CBPeripheralManager();


        public override void Start(AdvertisementData adData)
        {
            this.manager.StartAdvertising(new StartAdvertisingOptions
            {
                LocalName = adData.LocalName,
                ServicesUUID = adData
                    .ServiceUuids
                    .Select(x => CBUUID.FromString(x.ToString()))
                    .ToArray()
            });
            base.Start(adData);
        }


        public override void Stop()
        {
            this.manager.StopAdvertising();
            base.Stop();
        }
    }
}
#endif