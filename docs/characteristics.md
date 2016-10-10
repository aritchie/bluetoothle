# Characteristics

The most important thing with characteristics is NOT to store a reference to an object.  This reference becomes invalidated between connections to a device!


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

**Monitor Characteristic Read/Writes**
```csharp
// TODO
```

**Register for notifications on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
var sub = characteristic.SubscribeToNotifications().Subscribe(bytes => {});

sub.Dispose(); // to unsubscribe
```

**Monitor Characteristic Notifications**
```csharp
// once you have your characteristic instance from the service discovery
var sub = characteristic.WhenNotificationReceived().Subscribe(bytes => {});

sub.Dispose(); // to unsubscribe
```

**Discover descriptors on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
var sub = characteristic.WhenNotificationOccurs().Subscribe(bytes => {});

characteristic.WhenDescriptorsDiscovered().Subscribe(descriptor => {});
```


**Extensions**
```csharp

// read a characteristic on a given interval
characteristic.ReadInterval(TimeSpan).Subscribe(bytes => {});

// discover all characteristics
device.WhenAnyCharacteristic().Subscribe(characteristic => {});
