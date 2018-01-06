using System;


namespace Plugin.BluetoothLE
{
    public interface IGattResult
    {
        bool Success { get; }
        string ErrorMessage { get; }
        GattEvent Event { get; }
        byte[] Data { get; }
    }
}
