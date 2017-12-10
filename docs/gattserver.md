# GATT Server

Advertising is severally downplayed by both operating systems.  On iOS, you are limited to advertising the local name and service UUIDs

* On iOS, you can only advertise a custom local name and custom service UUIDs
* On Android, things are different
    * You cannot control the device naming
    * TX Power (high, medium, low, balanced)
    * Service Data
    * Service UUIDs
    * Specific Manufacturer Data

For now, I have chosen to support only the same feature band as iOS


```csharp

CrossBleAdapter.Current.Advertiser.Start(new AdvertisementData
{
    LocalName = "TestServer",
    ServiceUUIDs = new Guid[] { /* your custom UUIDs here */ }
});

CrossBleAdapter.Current.Advertiser.Stop();
```

# Services

Services are nothing more than categories in the overall perspective of BluetoothLE.  You should be aware that
you must setup all characters & descriptors that belong to a service BEFORE starting advertising or adding a service
to a running server!

You should always know your service UUID for future client consumption!

From a functionality perspective, there is not a lot you do with services

## Setup

```csharp
var server = CrossBleAdapter.Current.CreateGattServer();

// first parameter is your controlled GUID/UUID
// second parameter specifies if it is the primary service
server.CreateService(Guid.NewGuid(), true);

```


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