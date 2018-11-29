using System;
using System.Linq;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Plugin.BluetoothLE.Server.Internals;

namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        private readonly BluetoothManager manager;
        private readonly AdvertisementCallbacks adCallbacks;

        public Advertiser()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.adCallbacks = new AdvertisementCallbacks
            {
                Failed = (err) => IsStarted = false,
                Started = () => IsStarted = true
            };
        }

        public override void Start(AdvertisementData adData)
        {
            if (!CrossBleAdapter.AndroidConfiguration.IsServerSupported)
            {
                throw new BleException("BLE Advertiser needs API Level 21+");
            }

            if (IsStarted)
            {
                throw new BleException("BLE Advertiser is already started");
            }

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true);

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(adData.ManufacturerData == null && !(adData.ServiceUuids?.Any() ?? false)) //only when there is no other data, we'll get Data Too Long error otherwise
                .SetIncludeTxPowerLevel(true);

            if (adData.ManufacturerData != null)
            {
                data.AddManufacturerData(adData.ManufacturerData.CompanyId, adData.ManufacturerData.Data);
            }

            foreach (var serviceUuid in adData.ServiceUuids)
            {
                data.AddServiceUuid(serviceUuid.ToParcelUuid());
            }

            if (!string.IsNullOrEmpty(adData.LocalName))
            {
                manager.Adapter.SetName(adData.LocalName);
            }

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
            if (this.manager?.Adapter?.BluetoothLeAdvertiser != null && adCallbacks != null)
            {
                this.manager.Adapter.BluetoothLeAdvertiser.StopAdvertising(adCallbacks);
            }

            base.Stop();
        }
    }
}
