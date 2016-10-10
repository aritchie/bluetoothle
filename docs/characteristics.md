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

**Register for notifications on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
// this will enable the subscriptions to notifications as well as actually hook to the event
var sub = characteristic.SubscribeToNotifications().Subscribe(bytes => {});

sub.Dispose(); // to unsubscribe
```

**Monitor Characteristic Notifications**
```csharp
// once you have your characteristic instance from the service discovery
// this will only monitor notifications if they have been hooked by SubscribeToNotifications();
var sub = characteristic.WhenNotificationReceived().Subscribe(bytes => {});

sub.Dispose(); // to unsubscribe
```

**Monitor Reads/Writes**
```csharp
characteristic.WhenRead().Subscribe(bytes => {});
characteristic.WhenWritten().Subscribe(bytes => {});
```

**Discover descriptors on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery.

var sub = characteristic.WhenDescriptorsDiscovered().Subscribe(bytes => {});

characteristic.WhenDescriptorsDiscovered().Subscribe(descriptor => {});
```

## Extensions

```csharp

// read a characteristic on a given interval.  This is a substitute for SubscribeToNotifications()
characteristic.ReadInterval(TimeSpan).Subscribe(bytes => {});

// discover all characteristics without finding services first
device.WhenAnyCharacteristicDiscovered().Subscribe(characteristic => {});

// subscribe to ALL characteristic that notify (DANGER: you should really pick out your characteristics)
device.WhenAnyCharacteristicNotificationReceived().Subscribe(characterArgs => {});