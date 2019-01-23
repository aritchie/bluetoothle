# <img src="icon.png" width="71" height="71"/> ACR Reactive BluetoothLE Plugin
Easy to use, cross platform, REACTIVE BluetoothLE Plugin for ALL platforms!

# [![BSI Labs](bsilabs.png)](https://bsilabs.ca) 
This project is financially and technically supported by BSI Labs.

## [OTHER ACR PROJECTS](https://github.com/aritchie/home)

---

[Change Log - January 2019](docs/changelog.md)

[![NuGet](https://img.shields.io/nuget/v/Plugin.BluetoothLE.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.BluetoothLE/)
[![Build status](https://dev.azure.com/allanritchie/Plugins/_apis/build/status/BLE)](https://dev.azure.com/allanritchie/Plugins/_build/latest?definitionId=0)

## PLATFORMS

Platform|Version
--------|-------
Android|4.3+
iOS|7+
macOS|Latest
tvOS|Latest
Windows UWP|16299+

UWP is still in beta!
  * Client cannot disconnect
  * Server WIP
  * PRs only during beta please!


## FEATURES

* Scan for advertisement packets and devices (with full control of the scanning features)
* Monitor adapter status (and control it on android)
* Open Bluetooth settings screen
* Persistent connections
* Deals with the Android threading and defect headaches
* Discover services, characteristics, & descriptors
* Read, write, & receive notifications for characteristics
* Support for reliable write transactions
* Read & write descriptors
* Request & monitor MTU changes
* Connect to heart rate monitors
* Deals with most of the Android fubars
* Manages iOS backgrounding by allowing hooks to WhenWillRestoreState
* Control the adapter state on Android
* Pair with devices
* GATT Server and Advertising Support
    * Advertising
      * Manufactuer Data
      * Service UUIDs
    * Charactertistics
      * Read
      * Write
      * Notify & Broadcast
      * Manage Subscribers
      * Status Replies
* Android Issues
  * We manage the GATT 133 (mostly, hopefully)
  * Don't like the serial way you have to work with BLE, don't worry, we cover that too.  Read/Write away!
  * Don't know what thread to run a method on?  Don't worry - we got that covered.... just make the read/write call and relax


## SETUP

Be sure to install the Plugin.BluetoothLE nuget package in all of your main platform projects as well as your core/NETStandard project

[![NuGet](https://img.shields.io/nuget/v/Plugin.BluetoothLE.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.BluetoothLE/)

**Android**

Add the following to your AndroidManifest.xml
_PLEASE NOTE THAT YOU HAVE TO REQUEST THESE PERMISSIONS USING [Activity.RequestPermission](https://developer.android.com/training/permissions/requesting)_ or a [Plugin](https://github.com/jamesmontemagno/PermissionsPlugin)

```xml
<uses-permission android:name="android.permission.BLUETOOTH"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>

<!--this is necessary for Android v6+ to get the device name and address-->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />

```

**iOS**

If you want to use background BLE periperhals, add the following to your Info.plist

```xml

<key>UIBackgroundModes</key>
<array>
    <!--for connecting to devices (client)-->
	<string>bluetooth-central</string>

    <!--for server configurations if needed-->
	<string>bluetooth-peripheral</string>
</array>

<!--To add a description to the Bluetooth request message (on iOS 10 this is required!)-->
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```


## HOW TO USE - CLIENT BASICS

```csharp

// discover some devices
CrossBleAdapter.Current.Scan().Subscribe(scanResult => {});

// Once finding the device/scanresult you want
scanResult.Device.Connect();

Device.WhenAnyCharacteristicDiscovered().Subscribe(characteristic => {
    // read, write, or subscribe to notifications here
    var result = await characteristic.Read(); // use result.Data to see response
    await characteristic.Write(bytes);

    characteristic.EnableNotifications();
    characteristic.WhenNotificationReceived().Subscribe(result => {
    	//result.Data to get at response
    });
});

```


## HOW TO USE - SERVER BASICS

Most important things - you should setup all of your services and characteristics BEFORE you Start() the server!

```csharp
var server = CrossBleAdapter.Current.CreateGattServer();
var service = server.AddService(Guid.NewGuid(), true);

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

IDisposable notifyBroadcast = null;
notifyCharacteristic.WhenDeviceSubscriptionChanged().Subscribe(e =>
{
    var @event = e.IsSubscribed ? "Subscribed" : "Unsubcribed";

    if (notifyBroadcast == null)
    {
        this.notifyBroadcast = Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Where(x => notifyCharacteristic.SubscribedDevices.Count > 0)
            .Subscribe(_ =>
            {
                Debug.WriteLine("Sending Broadcast");
                var dt = DateTime.Now.ToString("g");
                var bytes = Encoding.UTF8.GetBytes(dt);
                notifyCharacteristic.Broadcast(bytes);
            });
    }
});

characteristic.WhenReadReceived().Subscribe(x =>
{
    var write = "HELLO";

    // you must set a reply value
    x.Value = Encoding.UTF8.GetBytes(write);

    x.Status = GattStatus.Success; // you can optionally set a status, but it defaults to Success
});
characteristic.WhenWriteReceived().Subscribe(x =>
{
    var write = Encoding.UTF8.GetString(x.Value, 0, x.Value.Length);
    // do something value
});

await server.Start(new AdvertisementData
{
    LocalName = "TestServer"
});
```


## DOCUMENTATION

* [Adapter](docs/adapter.md)
* [Device](docs/device.md)
* [Services](docs/services.md)
* [Characteristics](docs/characteristics.md)
* [Descriptors](docs/descriptors.md)
* Platform Specifics
    * [Android](docs/platform_android.md)
    * [iOS](docs/platform_ios.md)
    * [UWP](docs/platform_uwp.md)
* Extensions
    * [Heart Rate](docs/extensions_heartrate.md)
    * [Beacons](docs/extensions_beacons.md)
* Server
    * [Advertising](docs/advertising.md)
    * [GATT](docs/gattserver.md)

## FAQ

Q. Why is everything reactive instead of events/async

> I wanted event streams as I was scanning devices.  I also wanted to throttle things like characteristic notification feeds.  Lastly, was the proper cleanup of events and resources.

Q. Why are Device.Connect, Characteristic.Read, and Descriptor.Read observable when async would do just fine?

> True, but observables with RX are actually awaitable as well and far easier to chain into other things.

Q. Why are devices cleared on a new scan?

> Some platforms yield a "new" device and therefore new hooks.  This was observed on some android devices.

Q. My characteristic read/writes/notifications are not working

> If you store your discovered characteristics in your own variables, make sure to refresh them with each (re)connect

Q. I cannot see the device name in Android 6+

> You need to enable permissions for android.permission.ACCESS_COARSE_LOCATION 

Q. I cannot see the device name when scanning in the background on iOS

> This is the work of iOS.  The library cannot fix this.  You should scan by service UUIDs instead

Q. Does this support Bluetooth v2?

> No - please read about bluetooth specifications before using this library.  LE (Low Energy) is part of the v4.0 specification.

Q. Why can't I disconnect devices selectively in the GATT server?

> On android, you can, but exposing this functionality in xplat proves challenging since iOS does not support A LOT of things


Q. Why can't I configure the device name on Android?

> Please read the [advertising docs](docs/advertising.md) on this


## GENERAL RULES TO FOLLOW

* DO NOT reuse services, characteristics, and descriptors between connnections
* DO catch errors in your subscriptions (ie. Reads/Writes)
* DO set timeouts on all connected operations using [Observable.Timeout(TimeSpan)](http://www.introtorx.com/content/v1.0.10621.0/13_TimeShiftedSequences.html#Timeout).  Timeout throws errors that you must also manage!
* DO NOT manage reconnection yourself
* DO NOT scan with the adapter while you have an open GATT connection 
* If you have a TX/RX setup using Notify/Write, use 2 characteristics, not one

## CONTRIBUTORS

Thank you for all your help

* **[Marius Bloemhof](https://github.com/mariusbloemhof)** for all the extensive testing 
* **[Jesse Jiang](https://github.com/jessejiang0214)** for all the PRs
* **[Jelle Damen](https://twitter.com/JelleDamen)** for the wonderful icons
* **[MKGNZ](https://github.com/MKGNZ)** for the UWP work
