# ACR Reactive BluetoothLE Plugin for Xamarin
Easy to use, cross platform, REACTIVE BluetoothLE Plugin for Xamarin (Windows UWP COMING SOON)

[![NuGet](https://img.shields.io/nuget/v/Acr.Ble.svg?maxAge=2592000)](https://www.nuget.org/packages/Acr.Ble/)

[Change Log - Dec 15, 2016](docs/changelog.md)


## PLATFORMS

* Android 4.3+
* iOS 6+
* tvOS / Mac (support is here, I haven't had the means to fully test these yet)
* Windows UWP planned for v2 (the API is dev hostile and requires adjustments in the library interface to work with this)


## FEATURES

* Scan for advertisement packets and devices
* Monitor adapter status
* Connect to device and monitor status
* Discover services, characteristics, & descriptors
* Read, write, & receive notifications for characteristics
* Read & write descriptors
* Monitor all aspects of the BLE plugin via CreateLogging extension
* Connect to heart rate monitors


## SETUP

Be sure to install the Acr.Ble nuget package in all of your main platform projects as well as your core/PCL project

[![NuGet](https://img.shields.io/nuget/v/Acr.Ble.svg?maxAge=2592000)](https://www.nuget.org/packages/Acr.Ble/)

**Android**

Add the following to your AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.BLUETOOTH"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>

<!--this is necessary for Android v6+ to get the device name and address-->
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

**iOS**

If you want to use background BLE periperhals, add the following to your Info.plist

```xml
<array>
<string>bluetooth-central</string>
</array>

To add a description to the Bluetooth request message (on iOS 10 this is required!)
```xml
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```

## HOW TO USE BASICS

```csharp

// discover some devices
BleAdapter.Current.Scan().Subscribe(scanResult => {});

// Once finding the device/scanresult you want
await scanResult.Device.Connect();

Device.WhenAnyCharacteristicDiscovered().Subscribe(characteristic => {
    // read, write, or subscribe to notifications here
    var result = await characteristic.Read(); // use result.Data to see response
    await characteristic.Write(bytes);

    characteristic.SubscribeToNotifications(result => {
    	//result.Data to get at response
    });
});

```


## DOCUMENTATION

* [Adapter](docs/adapter.md)
* [Device](docs/device.md)
* [Services](docs/services.md)
* [Characteristics](docs/characteristics.md)
* [Descriptors](docs/descriptors.md)
* [Android Troubleshooting](docs/android.md)
* Plugins
    * [Logging](docs/logging.md)
    * [Heart Rate](docs/heartrate.md)


## FAQ

Q. Why is everything reactive instead of events/async

> I wanted event streams as I was scanning devices.  I also wanted to throttle things like characteristic notification feeds.  Lastly, was the proper cleanup of events and resources.

Q. Why are Device.Connect, Characteristic.Read, and Descriptor.Read observable when async would do just fine?

> True, but observables with RX are actually awaitable as well and far easier to chain into other things.

Q. Why have a Adapter.BackgroundScan with a service UUID?  This is not a problem on Android

> Also, true, but consistency is what I was aiming for.  iOS only allows you to scan in the background with a serviceUUID and on Android, I set the scanmode to low power.

Q. Why are devices cleared on a new scan?

> Some platforms yield a "new" device and therefore new hooks.  This was observed on some android devices.

Q. My characteristic read/writes/notifications are not working

> If you store your discovered characteristics in your own variables, make sure to refresh them with each (re)connect

Q. I cannot see the device name in Android 6+

> You need to enable permissions for android.permission.ACCESS_COARSE_LOCATION 

Q. I cannot see the device name when scanning in the background on iOS

> This is the work of iOS.  The library cannot fix this.  You should scan by service UUIDs instead