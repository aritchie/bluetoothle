#if __IOS__ || __MACOS__
using System;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly CBPeripheralManager manager;


        public Advertiser(CBPeripheralManager peripheralManager)
            => this.manager = peripheralManager;


        // needs to be async for errors
        public override void Start(AdvertisementData adData)
        {
            switch (this.manager.State)
            {
                case CBPeripheralManagerState.Resetting:
                case CBPeripheralManagerState.Unknown:
                    this.manager.StateUpdated += (sender, args) =>
                    {
                        if (this.manager.State == CBPeripheralManagerState.PoweredOn)
                            this.DoAdvertise(adData);
                    };
                    break;

                case CBPeripheralManagerState.PoweredOn:
                    this.DoAdvertise(adData);
                    break;

                case CBPeripheralManagerState.PoweredOff:
                    break;

                case CBPeripheralManagerState.Unsupported:
                    break;

                case CBPeripheralManagerState.Unauthorized:
                    // TODO: exception?
                    break;
            }
        }


        void DoAdvertise(AdvertisementData adData)
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