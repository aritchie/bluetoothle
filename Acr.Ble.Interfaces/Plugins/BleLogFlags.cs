using System;


namespace Acr.Ble.Plugins
{
    [Flags]
    public enum BleLogFlags
    {
        AdapterStatus = 1,
        AdapterScanStatus = 2,
        AdapterScanResults = 4,
        AdapterAll = AdapterStatus | AdapterScanStatus | AdapterScanResults,

        DeviceStatus = 8,

        ServiceDiscovered = 16,

        CharacteristicDiscovered = 32,
        CharacteristicRead = 64,
        CharacteristicWrite = 128,
        CharacteristicNotify = 256,
        CharacteristicAll = CharacteristicDiscovered | CharacteristicRead | CharacteristicWrite | CharacteristicNotify,

        DescriptorDiscovered = 512,
        DescriptorRead = 1024,
        DescriptorWrite = 2048,
        DescriptorAll = DescriptorRead | DescriptorWrite | DescriptorWrite,

        All = AdapterAll | DeviceStatus | ServiceDiscovered | CharacteristicAll | DescriptorAll
    }
}
