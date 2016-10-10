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