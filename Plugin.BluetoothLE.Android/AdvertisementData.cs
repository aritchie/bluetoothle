using System;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth.LE;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly Lazy<InternalScanRecord> lazyParse;


        public AdvertisementData(ScanRecord scanRecord) : this(scanRecord.GetBytes())
        {
        }


        public AdvertisementData(byte[] scanRecord)
        {
            this.lazyParse = new Lazy<InternalScanRecord>(() => InternalScanRecord.Parse(scanRecord));
        }


        public string LocalName => this.lazyParse.Value.LocalName;
        public bool IsConnectable => this.lazyParse.Value.IsConnectable;
        public byte[] ManufacturerData => this.lazyParse.Value.ManufacturerData;
        public Guid[] ServiceUuids => this.lazyParse.Value.ServiceUuids.ToArray();
        public IReadOnlyList<byte[]> ServiceData => this.lazyParse.Value.ServiceData;
        public int TxPower => this.lazyParse.Value.TxPower;
    }
}