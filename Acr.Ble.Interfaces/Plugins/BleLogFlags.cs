using System;


namespace Acr.Ble.Plugins
{
    [Flags]
    public enum BleLogFlags
    {
        AdapterStatus = 1,
        AdapterScanStatus = 2,
        AdapterScanResults = 4,

        DeviceConnected = 8,
        DeviceDisconnected = 16,

        ServiceDiscovered = 32,

        CharacteristicDiscovered = 64,
        CharacteristicRead = 128,
        CharacteristicWrite = 256,
        CharacteristicNotify = 512,

        DescriptorDiscovered = 1024,
        DescriptorRead = 2048,
        DescriptorWrite = 4096,

        AdapterAll = AdapterStatus | AdapterScanStatus | AdapterScanResults,
        DeviceStatus = DeviceConnected | BleLogFlags.DeviceDisconnected,
        CharacteristicAll = CharacteristicDiscovered | CharacteristicRead | CharacteristicWrite | CharacteristicNotify,
        DescriptorAll = DescriptorDiscovered | DescriptorRead | DescriptorWrite,
        All = AdapterAll | DeviceStatus | ServiceDiscovered | CharacteristicAll | DescriptorAll
    }
}
