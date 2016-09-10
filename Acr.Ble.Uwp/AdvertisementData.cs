using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;


namespace Acr.Ble
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly BluetoothLEAdvertisement adData;


        public AdvertisementData(BluetoothLEAdvertisement adData)
        {
            this.adData = adData;
            //adData.ManufacturerData[0].Data.ToArray();
            //this.adData.GetSectionsByType((byte)AdvertisementRecordType.TxPowerLevel)
        }

        public string LocalName => this.adData.LocalName;
        public bool IsConnectable { get; }
        public byte[] ManufacturerData { get; }
        public Guid[] ServiceUuids => this.adData.ServiceUuids.ToArray();
        public int TxPower { get; }
    }
}
