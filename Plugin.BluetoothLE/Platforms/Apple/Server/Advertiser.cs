#if __IOS__ || __MACOS__
using System;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly Adapter adapter;

        public Advertiser(Adapter adapter)
            => this.adapter = this.adapter;


        // needs to be async for errors
        public override void Start(AdvertisementData adData)
        {
            if (this.adapter?.PeripheralManager?.Advertising ?? true)
                return;

            adapter.WaitForPeripheralManagerIfNeedeed();
            if (!adapter.IsPeripheralManagerTurnedOn())
                throw new BleException("Invalid Adapter State - " + adapter.PeripheralManager.State);

            this.DoAdvertise(adData);
            //switch (this.manager.State)
            //{
            //    case CBPeripheralManagerState.Resetting:
            //    case CBPeripheralManagerState.Unknown:
            //        this.manager.StateUpdated += (sender, args) =>
            //        {
            //            if (!this.manager.Advertising && this.manager.State == CBPeripheralManagerState.PoweredOn)
            //                this.DoAdvertise(adData);
            //        };
            //        break;

            //    case CBPeripheralManagerState.PoweredOn:
            //        this.DoAdvertise(adData);
            //        break;

            //    case CBPeripheralManagerState.PoweredOff:
            //        break;

            //    case CBPeripheralManagerState.Unsupported:
            //        break;

            //    case CBPeripheralManagerState.Unauthorized:
            //        // TODO: exception?
            //        break;
            //}
            base.Start(adData);
        }


        void DoAdvertise(AdvertisementData adData)
        {
            this.adapter?.PeripheralManager?.StartAdvertising(new StartAdvertisingOptions
            {
                LocalName = adData.LocalName,
                ServicesUUID = adData
                    .ServiceUuids
                    .Select(x => CBUUID.FromString(x.ToString()))
                    .ToArray()
            });
        }


        public override void Stop()
        {
            this.adapter?.PeripheralManager?.StopAdvertising();
            base.Stop();
        }
    }
}
#endif