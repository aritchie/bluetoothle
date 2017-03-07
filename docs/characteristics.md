# Characteristics

Characteristics are the heart and soul of the BLE GATT connections.  You can read, write, and monitor the values (depending on permissions) with this plugin.

_It important thing with characteristics is NOT to store a reference to an object.  This reference becomes invalidated between connections to a device!_

## General Usage

**Discover characteristics on service**
```csharp
Service.WhenCharacteristicDiscovered().Subscribe(characteristic => {});
```

**Read and Write to a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
await Characteristic.Read();

await Characteristic.Write(bytes);
```

**Write Without Response**
```csharp
Characteristic.WriteWithoutResponse(bytes);
```

**Register for notifications on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
// this will enable the subscriptions to notifications as well as actually hook to the event
var sub = characteristic.SubscribeToNotifications().Subscribe(result => { result.Data... });

sub.Dispose(); // to unsubscribe
```

**Monitor Characteristic Notifications**
```csharp
// once you have your characteristic instance from the service discovery
// this will only monitor notifications if they have been hooked by SubscribeToNotifications();
var sub = characteristic.WhenNotificationReceived().Subscribe(result => { result.Data... });

sub.Dispose(); // to unsubscribe
```

**Monitor Reads/Writes**
```csharp
characteristic.WhenRead().Subscribe(result => { result.Data... });
characteristic.WhenWritten().Subscribe(result => { result.Data... });
```

**Discover descriptors on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery.

var sub = characteristic.WhenDescriptorsDiscovered().Subscribe(bytes => {});

characteristic.WhenDescriptorsDiscovered().Subscribe(descriptor => {});
```

**BLOB Writes**

Used for sending larger arrays or streams of data without working with the MTU byte gap

```csharp
characteristic.BlobWrite(stream).Subscribe(x => 
{
	// subscription will give you current position and pulse every time a buffer is written
	// if write no response is used, a 100ms gap is placed after each write.  Note that this event will fire quicker as well
});


// same as above but with a byte array
characteristic.BlobWrite(bytes).Subscribe(x => {}); 
```

**Reliable Write Transactions**
```csharp
// TODO
```

## Extensions

```csharp

// read a characteristic on a given interval.  This is a substitute for SubscribeToNotifications()
characteristic.ReadInterval(TimeSpan).Subscribe(result => { result.Data... });

// discover all characteristics without finding services first
device.WhenAnyCharacteristicDiscovered().Subscribe(characteristic => {});

// will continue to read in a loop until ending bytes (argument) is detected
device.ReadUntil(new byte[] { 0x0 }).Subscribe(result => { result.Data... });
```
