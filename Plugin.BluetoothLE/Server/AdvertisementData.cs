using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE.Server
{
    public class AdvertisementData
    {
        public bool AndroidUseDeviceName { get; set; } = false;

        /// <summary>
        /// If you enable the GATT server, setting this flag is ignored
        /// </summary>
        public bool AndroidIsConnectable { get; set; } = true;


        public string LocalName { get; set; }
        public ManufacturerData ManufacturerData { get; set; }
        public List<Guid> ServiceUuids { get; set; } = new List<Guid>();

        // ANDROID ONLY
        public bool AndroidIncludeDeviceName { get; set; }
        public bool AndroidIncludeTxPower { get; set; }
        //public IDictionary<Guid, byte[]> ServiceData { get; set; } = new Dictionary<Guid, byte[]>();
    }
}