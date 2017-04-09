using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE.Server
{
    public class AdvertisementData
    {
        public string LocalName { get; set; }
        public ManufacturerData ManufacturerData { get; set; }
        public List<Guid> ServiceUuids { get; set; } = new List<Guid>();

        // ANDROID ONLY
        //public bool IncludeDeviceName { get; set; }
        //public bool IncludeTxPower { get; set; }
        //public IDictionary<Guid, byte[]> ServiceData { get; set; } = new Dictionary<Guid, byte[]>();
    }
}