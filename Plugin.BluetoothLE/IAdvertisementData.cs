using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE
{
    public interface IAdvertisementData
    {
        string LocalName { get; }
        bool IsConnectable { get; }
        IReadOnlyList<byte[]> ServiceData { get; }
        IEnumerable<ManufacturerData> ManufacturerData { get; }
        Guid[] ServiceUuids { get; }
        int TxPower { get; }
    }
}
