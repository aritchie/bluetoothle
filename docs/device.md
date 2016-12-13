# Device

This section deals with gatt connections and state monitoring for a device.
You should maintain a reference to a device if you intend to connect to it.


**Connect/Disconnect to a device**

```csharp
// connect
await device.Connect(); // this is an observable so you can do other funky timeouts

device.Disconnect();
```

**Pairing with a device**
```csharp
if (device.IsPairingRequestSupported && device.PairingStatus != PairingStatus.Paired) 
{
    // there is an optional argument to pass a PIN in PairRequest as well
    device.PairRequest().Subscribe(isSuccessful => {});
}
```


**Monitor device states**

```csharp
// This will tell you if the device name changes during a connection
device.WhenNameChanged().Subscribe(string => {});

// this will monitor the RSSI of the connected device.  This will attempt to pull the RSSI every 3 seconds by default
device.WhenRssiChanged().Subscribe(rssi => {});

// this will watch the connection states to the device
device.WhenStatusChanged().Subscribe(connectionState => {});
```


**Smart/Persistent Connection**

_CreateConnection creates a resilient connection that will immediately attempt to reconnect if disconnected as long as you maintain a reference to the observable subscription_

```csharp
var connection = device.CreateConnection().Subscribe(connectionState => 
{
    // you can see the connection transitions here
    // dont try to manage reconnections, this guy will do it for you!
});

// this will close the connection and stop reconnection attempts.
// the GC can also get at this for you!
connection.Dispose();  

```