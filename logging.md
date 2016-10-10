# Logging

Logging allows you to monitor any/all categories of the BLE plugin.

```csharp
adapter
    .CreateLogger(BleLogFlags.All)
    .Subscribe(record => {})
```
