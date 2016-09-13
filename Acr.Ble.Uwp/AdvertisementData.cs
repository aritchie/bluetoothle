using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;


namespace Acr.Ble
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly BluetoothLEAdvertisement adData;


        public AdvertisementData(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.adData = args.Advertisement;

            switch (args.AdvertisementType)
            {
                case BluetoothLEAdvertisementType.ConnectableDirected:
                case BluetoothLEAdvertisementType.ConnectableUndirected:
                    this.IsConnectable = true;
                    break;

                default:
                    this.IsConnectable = false;
                    break;
            }
            //adData.ManufacturerData[0].Data.ToArray();
            //this.adData.GetSectionsByType((byte)AdvertisementRecordType.TxPowerLevel);
        }

        public string LocalName => this.adData.LocalName;
        public bool IsConnectable { get; }
        public byte[] ManufacturerData { get; }
        public Guid[] ServiceUuids => this.adData.ServiceUuids.ToArray();
        public int TxPower { get; }
    }
}
