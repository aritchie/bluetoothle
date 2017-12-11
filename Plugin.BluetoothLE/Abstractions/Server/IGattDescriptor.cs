using System;


namespace Plugin.BluetoothLE.Server
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }
        Guid Uuid { get; }
        byte[] Value { get; }
    }
}
