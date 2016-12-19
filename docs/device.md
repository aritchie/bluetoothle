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

**Request MTU size increase**


**MTU (Max Transmission Unit)**
If MTU requests are available (Android Only - API 21+)

This is specific to Android only where this negotiation is not automatic.
The size can be up to 512, but you should be careful with anything above 255 in practice
```csharp
// Request a greater MTU size (Androd API 21+ only)
device.RequestMtu(255);

// iOS will return current, Android will return 20 unless changes are observed
device.GetCurrentMtu();

// iOS will return current value and return, Android will continue to monitor changes
device.WhenMtuChanged().Subscribe(...)
```

**Monitor device states**

```csharp
// This will tell you if the device name changes during a connection
device.WhenNameChanged().Subscribe(string => {});

// this will monitor the RSSI of the connected device.  This will attempt to pull the RSSI every 3 seconds by default
device.WhenRssiChanged().Subscribe(rssi => {});

// this will watch the connection states to the device
device.WhenStatusChanged().Subscribe(connectionState => {});

// monitor MTU size changes (droid only)
device.WhenMtuChanged().Subscribe(size => {});

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