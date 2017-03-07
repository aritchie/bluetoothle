# ACR Reactive BluetoothLE Plugin for Xamarin
Easy to use, cross platform, REACTIVE BluetoothLE Plugin for iOS, Android, macOS & tvOS

## Please note that v2.0 of this plugin has had its namespace and assembly renamed to Plugin.BluetoothLE.  If you are having any difficulties with the install, uninstall Acr.Ble and continue with the Plugin.BluetoothLE nuget package

[![NuGet](https://img.shields.io/nuget/v/Plugin.BluetoothLE.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.BluetoothLE/)

[Change Log - Jan 19, 2017](docs/changelog.md)


## PLATFORMS

* Android 4.3+
* iOS 7+
* macOS
* tvOS
* Windows UWP (COMING SOON)


## FEATURES

* Scan for advertisement packets and devices (with full control of the scanning features)
* Monitor adapter status (and control it on android)
* Open Bluetooth settings screen
* Persistent connections
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


## SETUP

Be sure to install the Acr.Ble nuget package in all of your main platform projects as well as your core/PCL project

[![NuGet](https://img.shields.io/nuget/v/Plugin.BluetoothLE.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.BluetoothLE/)

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
CrossBleAdapter.Current.Scan().Subscribe(scanResult => {});

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
* Platform Specifics
    * [Android](docs/android.md)
    * [iOS](docs/ios.md)
* Extensions
    * [Heart Rate](docs/heartrate.md)


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
