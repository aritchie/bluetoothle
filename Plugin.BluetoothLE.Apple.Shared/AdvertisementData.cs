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
        readonly Lazy<List<byte[]>> serviceData;


        public AdvertisementData(NSDictionary adData)
        {
            this.adData = adData;
            this.localName = this.GetLazy(CBAdvertisement.DataLocalNameKey, x => x.ToString());
            this.connectable = this.GetLazy(CBAdvertisement.IsConnectable, x => ((NSNumber)x).Int16Value == 1);
            this.txpower = this.GetLazy(CBAdvertisement.DataTxPowerLevelKey, x => Convert.ToInt32(((NSNumber)x).Int16Value));
            this.manufacturerData = this.GetLazy(CBAdvertisement.DataManufacturerDataKey, x => ((NSData)x).ToArray());
            this.serviceData = this.GetLazy(CBAdvertisement.DataServiceDataKey, item =>
            {
                var data = (NSDictionary)item;
                var list = new List<byte[]>();

                foreach (CBUUID key in data.Keys)
                {
                    var rawKey = key.Data.ToArray();
                    var rawValue = ((NSData)data.ObjectForKey(key)).ToArray();

                    Array.Reverse(rawKey);
                    var result = new byte[rawKey.Length + rawValue.Length];
                    Buffer.BlockCopy(rawKey, 0, result, 0, rawKey.Length);
                    Buffer.BlockCopy(rawValue, 0, result, rawKey.Length, rawValue.Length);

                    list.Add(result);
                }
                return list;
            });
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
        public IReadOnlyList<byte[]> ServiceData => this.serviceData.Value;
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