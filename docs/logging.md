# Logging

Logging allows you to monitor any/all categories of the BLE plugin.  You can use the BleLogFlags to control what events you want to observe

## Hook to logging
```csharp
adapter
    .CreateLogger(BleLogFlags.All)
    .Subscribe(record => {})
```

## Specific Logging
```csharp
adapter
    .CreateLogger(BleLogFlags.CharacteristicRead | BleLogFlags.CharacteristicWrite | BleLogFlags.Notification)
    .Subscribe(record => {});
```

## Logging Levels
```csharp
[Flags]
public enum BleLogFlags
{
    AdapterStatus = 1,
    AdapterScanStatus = 2,
    AdapterScanResults = 4,

    DeviceStatus = 8,

    ServiceDiscovered = 16,

    CharacteristicDiscovered = 32,
    CharacteristicRead = 64,
    CharacteristicWrite = 128,
    CharacteristicNotify = 256,

    DescriptorDiscovered = 512,
    DescriptorRead = 1024,
    DescriptorWrite = 2048,

    AdapterAll = AdapterStatus | AdapterScanStatus | AdapterScanResults,
    CharacteristicAll = CharacteristicDiscovered | CharacteristicRead | CharacteristicWrite | CharacteristicNotify,
    DescriptorAll = DescriptorDiscovered | DescriptorRead | DescriptorWrite,
    All = AdapterAll | DeviceStatus | ServiceDiscovered | CharacteristicAll | DescriptorAll
}
```