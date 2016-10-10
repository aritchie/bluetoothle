# Adapter

**Monitor and read status of adapter**
```csharp
BleAdapter.Current.Status

BleAdapter.Current.WhenStatusChanged().Subscribe(status => {});

```

**Scan for Devices**

```csharp
var scanner = BleAdapter.Current.Scan().Subscribe(scanResult => 
{
    // do something with it
    // the scanresult contains the device, RSSI, and advertisement packet
        
});

scanner.Dispose(); // to stop scanning
```

**Listen to scans for decoupled components
With the use of observables everywhere, the option to hook up to the scan result events were taken away.  There are good cases to have listening options without actually starting a scan.  This is that option!
```csharp

BleAdapter.Current.ScanListen().Subscribe(scanResult => {});


```
