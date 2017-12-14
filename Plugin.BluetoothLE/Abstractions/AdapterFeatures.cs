using System;


namespace Plugin.BluetoothLE
{
    [Flags]
    public enum AdapterFeatures
    {
        None = -1,
        ControlAdapterState = 1,
        OpenSettings = 2,
        ViewPairedDevices = 4,
        LowPoweredScan = 8,

        ServerAdvertising = 16,
        ServerGatt = 32,
        // AdvertiseManufacturerData

        AllClient = ViewPairedDevices | LowPoweredScan,
        AllServer = ServerAdvertising | ServerGatt,
        AllControls = ControlAdapterState | OpenSettings,

        All = AllClient | AllServer | AllControls
    }
}
