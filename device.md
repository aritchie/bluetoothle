# Device

**Connect/Disconnect to a device**

```csharp
// connect
await device.Connect(); // this is an observable so you can do other funky timeouts

device.Disconnect();
```


**Monitor device states**

```csharp
// This will tell you if the device name changes during a connection
device.WhenNameChanged().Subscribe(string => {});

// this will monitor the RSSI of the connected device
device.WhenRssiChanged().Subscribe(rssi => {});

// this will watch the connection states to the device
device.WhenStatusChanged().Subscribe(connectionState => {});
```


**Smart/Persistent Connection**

```csharp
var connection = device.PersistentConnect().Subscribe(connectionState => 
{
    // you can see the connection transitions here
    // dont try to manage reconnections, this guy will do it for you!
});

// this will close the connection and stop reconnection attempts.
//The GC can also get at this for you!
connection.Dispose();  

```


**Discover services on a device**

```csharp
// once you have successfully scanned for a device, use the instance to discover services
// NOTE: you can call this repeatedly during the connection lifecycle to see all of the discovered services
Device.WhenServicesDiscovered().Subscribe(service => 
{
});
```