using System;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Plugin.BluetoothLE.Server.Internals;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly BluetoothManager manager;
        readonly AdvertisementCallbacks adCallbacks;


        public Advertiser()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.adCallbacks = new AdvertisementCallbacks();
        }


        public override void Start(AdvertisementData adData)
        {
            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true);

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(true)
                .SetIncludeTxPowerLevel(true);

            if (adData.ManufacturerData != null)
                data.AddManufacturerData(adData.ManufacturerData.CompanyId, adData.ManufacturerData.Data);

            foreach (var serviceUuid in adData.ServiceUuids)
                data.AddServiceUuid(serviceUuid.ToParcelUuid());

            this.manager
                .Adapter
                .BluetoothLeAdvertiser
                .StartAdvertising(
                    settings.Build(),
                    data.Build(),
                    this.adCallbacks
                );

            base.Start(adData);
        }


        public override void Stop()
        {
            this.manager.Adapter.BluetoothLeAdvertiser.StopAdvertising(this.adCallbacks);
            base.Stop();
        }
    }
}
