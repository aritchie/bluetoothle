using System;
using Windows.Devices.Bluetooth.Advertisement;


namespace Acr.Ble
{
    public class AdvertisementData : IAdvertisementData
    {
        public AdvertisementData(BluetoothLEAdvertisement adData)
        {
            //this.ServiceUuids = adData.ServiceUuids.Select(x => x.ToString()).ToArray();

            //this.ManufacturerData = adData.ManufacturerData.FirstOrDefault()?.Data.ToArray();
        }

        public string LocalName { get; }
        public bool IsConnectable { get; }
        public byte[] ManufacturerData { get; }
        public Guid[] ServiceUuids { get; }
        public int TxPower { get; }
    }
}
