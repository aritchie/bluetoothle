using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;


namespace Acr.Ble
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly BluetoothLEAdvertisementReceivedEventArgs adData;
        readonly Lazy<byte[]> manufacturerData;
        readonly Lazy<int> txPower;


        public AdvertisementData(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.adData = args;
            this.IsConnectable = args.AdvertisementType == BluetoothLEAdvertisementType.ConnectableDirected ||
                                 args.AdvertisementType == BluetoothLEAdvertisementType.ConnectableUndirected;

            this.manufacturerData = this.GetLazy(AdvertisementRecordType.ManufacturerSpecificData, sections =>
            {
                var data = sections.Last().Data.ToArray();
                return data;
            });
            this.txPower = this.GetLazy<int>(AdvertisementRecordType.TxPowerLevel, sections =>
            {
                var bytes = sections.Last().Data.ToArray();
                return bytes[0];
            });
        }


        public ulong BluetoothAddress => this.adData.BluetoothAddress;
        public string LocalName => this.adData.Advertisement.LocalName;
        public bool IsConnectable { get; }
        public byte[] ManufacturerData => this.manufacturerData.Value;
        public Guid[] ServiceUuids => this.adData.Advertisement.ServiceUuids.ToArray();
        public int TxPower => this.txPower.Value;


        Lazy<TResult> GetLazy<TResult>(AdvertisementRecordType recordType, Func<IReadOnlyList<BluetoothLEAdvertisementDataSection>, TResult> parseAction)
        {
            return new Lazy<TResult>(() =>
            {
                var sections = this.adData.Advertisement.GetSectionsByType((byte) recordType);
                if (!sections?.Any() ?? true)
                    return default(TResult);

                var result = parseAction(sections);
                return result;
            });
        }
    }
}
