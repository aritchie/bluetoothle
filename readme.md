# ACR Reactive BluetoothLE Plugin for Xamarin & Windows
Easy to use, cross platform, REACTIVE BluetoothLE Plugin for Xamarin (Windows UWP COMING SOON)

## PLATFORMS

* Android 4.3+
* iOS 6+
* Windows UWP (COMING SOON)

## SETUP

### Android

    Add the following to your AndroidManifest.xml
    <uses-permission android:name="android.permission.BLUETOOTH"/>
    <uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>

### iOS

    If you want to use background BLE periperhals, add the following to your Info.plist
    <array>
	<string>bluetooth-central</string>

### Windows

    Add to your app manifest file
    <Capabilities>
        <Capability Name="internetClient" />
        <DeviceCapability Name="bluetooth" />
    </Capabilities>

## HOW TO USE

### Scan for Devices

    var scanner = BleService.Adapter.Scan().Subscribe(scanResult => {
        // do something with it
    });

    scanner.Dispose(); // to stop scanning

### Discover services on a device

    //Once you have successfully scanned for a device, use the instance

    Device.WhenServicesDiscovered().Subscribe(service => 
    {
        service.W
    });



### Discover characteristics on service

    Service.

### Read and Write to a characteristic

### Register for Notifications on a characteristic

### Discover descriptors on a characteristic


## FAQ

Q. Why is everything reactive instead of events/async
A. I wanted event streams as I was scanning devices.  I also wanted to throttle things like characteristic notification feeds.  Lastly, was the proper cleanup of events and resources.   

