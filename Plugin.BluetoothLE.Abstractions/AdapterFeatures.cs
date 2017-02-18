using System;


namespace Plugin.BluetoothLE
{
    [Flags]
    public enum AdapterFeatures
    {
        None = 0,
        ControlAdapterState = 1,
        OpenSettings = 2,
        ViewPairedDevices = 4,
        LowPoweredScan = 8,

        All = ControlAdapterState | OpenSettings | ViewPairedDevices | LowPoweredScan
    }
}
