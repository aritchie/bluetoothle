using System;


namespace Plugin.BluetoothLE.Server
{
    [Flags]
    public enum GattPermissions
    {
        Read = 1,
        ReadEncrypted = 4,
        Write = 2,
        WriteEncrypted = 8
    }
}