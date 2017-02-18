using System;


namespace Plugin.BluetoothLE
{
    public interface IAdvertisementData
    {
        string LocalName { get; }
        bool IsConnectable { get; }
        //byte[] ServiceData { get; }
        byte[] ManufacturerData { get; }
        Guid[] ServiceUuids { get; }
        int TxPower { get; }
    }
}
