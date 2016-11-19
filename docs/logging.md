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
```

## Device Logging
```csharp

// you can also log at a device Levels

device.CreateLogger(levels).Subscribe(x => {});

```