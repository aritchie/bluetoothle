#if __IOS__ || __MACOS__
using System;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly Adapter adapter;
        IDisposable currentStartSubscription;

        public Advertiser(Adapter adapter)
            => this.adapter = adapter;

        // needs to be async for errors
        public override void Start(AdvertisementData adData)
        {
            if (this.adapter?.PeripheralManager == null)
                throw new BleException("PeripheralManager is not found!");
            
            if (this.adapter.PeripheralManager.Advertising)
                return;

            currentStartSubscription?.Dispose();

            currentStartSubscription = adapter.GetPeripheralManagerState()
                .Subscribe(state =>
                {
                    if (state != CBPeripheralManagerState.PoweredOn)
                        throw new BleException("Invalid Adapter State - " + adapter.PeripheralManager.State);

                    this.adapter?.PeripheralManager?.StartAdvertising(new StartAdvertisingOptions
                    {
                        LocalName = adData.LocalName,
                        ServicesUUID = adData
                            .ServiceUuids
                            .Select(x => CBUUID.FromString(x.ToString()))
                            .ToArray()
                    });

                    base.Start(adData);
                });
        }


        public override void Stop()
        {
            currentStartSubscription?.Dispose();

            this.adapter?.PeripheralManager?.StopAdvertising();
            base.Stop();
        }
    }
}
#endif