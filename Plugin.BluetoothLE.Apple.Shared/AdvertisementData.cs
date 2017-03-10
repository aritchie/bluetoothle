using System;
using System.Collections.Generic;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly NSDictionary adData;
        readonly Lazy<string> localName;
        readonly Lazy<bool> connectable;
        readonly Lazy<byte[]> manufacturerData;
        readonly Lazy<int> txpower;
        readonly Lazy<Guid[]> serviceUuids;


        public AdvertisementData(NSDictionary adData)
        {
            this.adData = adData;
            this.localName = this.GetLazy(CBAdvertisement.DataLocalNameKey, x => x.ToString());
            this.connectable = this.GetLazy(CBAdvertisement.IsConnectable, x => ((NSNumber)x).Int16Value == 1);
            this.txpower = this.GetLazy(CBAdvertisement.DataTxPowerLevelKey, x => Convert.ToInt32(((NSNumber)x).Int16Value));
            this.manufacturerData = this.GetLazy(CBAdvertisement.DataManufacturerDataKey, x => ((NSData)x).ToArray());
            this.serviceUuids = this.GetLazy(CBAdvertisement.DataServiceUUIDsKey, x =>
            {
                var array = (NSArray)x;
                var list = new List<Guid>();
                for (nuint i = 0; i < array.Count; i++)
                {
                    var guid = array.GetItem<CBUUID>(i).ToGuid();
                    list.Add(guid);
                }
                return list.ToArray();
            });
        }


        public string LocalName => this.localName.Value;
        public bool IsConnectable => this.connectable.Value;
        public byte[] ManufacturerData => this.manufacturerData.Value;
        public Guid[] ServiceUuids => this.serviceUuids.Value;
        public int TxPower => this.txpower.Value;


        protected Lazy<T> GetLazy<T>(NSString key, Func<NSObject, T> transform)
        {
            return new Lazy<T>(() =>
            {
                var obj = this.GetObject(key);
                if (obj == null)
                    return default(T);

                var result = transform(obj);
                return result;
            });
        }


        protected NSObject GetObject(NSString key)
        {
            if (this.adData == null)
                return null;

            if (!this.adData.ContainsKey(key))
                return null;

            return this.adData.ObjectForKey(key);
        }
    }
}