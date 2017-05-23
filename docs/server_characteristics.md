# Characteristics

These are the heart and soul of BLE.  This is where data is exchanged between client & server

## General Setup

After creating your server instance and a service, you can do the following:

You should always assign a known GUID ID to your characteristic in order for your GATT service to be consumed by a client.

Below are examples of a basic read/write characteristic and a notification characteristic setup

```csharp
var characteristic = service.AddCharacteristic(
    Guid.NewGuid(),
    CharacteristicProperties.Read | CharacteristicProperties.Write | CharacteristicProperties.WriteWithoutResponse,
    GattPermissions.Read | GattPermissions.Write
);

var notifyCharacteristic = service.AddCharacteristic
(
    Guid.NewGuid(),
    CharacteristicProperties.Indicate | CharacteristicProperties.Notify,
    GattPermissions.Read | GattPermissions.Write
);

```


### Read

When you have a read characteristic, you need to hook on to its read observable

```csharp
characteristic.WhenReadReceived().Subscribe(x =>
{
    var write = "HELLO";

    // you must set a reply value
    x.Value = Encoding.UTF8.GetBytes(write);

    x.Status = GattStatus.Success; // you can optionally set a status, but it defaults to Success
});
```

### Write

Same with write characteristic

```csharp
characteristic.WhenWriteReceived().Subscribe(x =>
{
    var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
    // do something with value

    // note that you can reply with a value and status here as well (like a read)
});
```

### Notification Subscriptions

It is important to know how many subscribers you have to know if you should be processing notifications 

Subscribers are nothing more than device UUIDs for identification.  From an IOS perspective, there is almost zero you can do with them

```csharp
notifyCharacteristic.WhenDeviceSubscriptionChanged().Subscribe(e =>
{
    var @event = e.IsSubscribed ? "Subscribed" : "Unsubcribed";
}

// you can also see a current list of subscribers

notifyCharacteristic.SubscribedDevices
```

### Broadcasting

Now that you have some subscribers, lets say hello every second just for fun
```csharp
notifyCharacteristic.Broadcast(bytes).Subscribe(); // passing no devices will cause a mass broadcast

// or you can pass to a select list of subscribers
notifyCharacteristic.Broadcast(bytes, notifyCharacteristic.SubscribedDevices.First());

// you can also listen to the callbacks for each broadcast
notifyCharacteristic.Broadcast(bytes);

// you can also listen to the callbacks for each broadcast
notifyCharacteristic.BroadcastObserve(bytes).Subscribe(info => {});
```