# Android

Android Bluetooth is painful and that's being nice.  This library attempts to deal with the necessary thread handling all internally.

All of the classes and members listed in this page can only be called from your Android project, not your PCL/Core library.  You should call and set
these values at your main launcher activity or even at an application level.
```

## Connection Options

Using androidAutoConnect is suggested in scenarios where you don't know if the device is in-range
This will cause Android to connect when it sees the device.  WARNING: initial connections take much
longer with this option enabled

```csharp

var device = CrossBleAdapter.Current.GetKnownDevice(guid);
device.Connect(new GattConnectionConfig {
	AndroidAutoConnect = true
})
.Subscribe();
```

## Settings

```csharp
The following values are only available to be set from your android project

// returns a suggestion on what thread to execute on.  This is not used internally
CrossBleAdapter.MainThreadSuggested { get; }

// defaults to MainThreadSuggested.  Background actions most android devices seem to throw an exception if not connected on the main thread
CrossBleAdapter.PerformActionsOnMainThread = true; 
```