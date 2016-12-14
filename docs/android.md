# Android

At some point, you may have an older device or pre-lollipop OS to deal with.  This library can only offer ways to help you deal with it, however,
you will have to tell it how to deal with it on a per device basis.  There is a helper method listed below that will offer a suggestion for how
to manage the connection thread.

All of the classes and members listed in this page can only be called from your Android project, not your PCL/Core library.  You should call and set
these values at your main launcher activity or even at an application level.

## Config Values
```csharp
public enum ConnectionThread
{
    /// <summary>
    /// Allow RX to delegate a thread
    /// </summary>
    Default = 0,

    /// <summary>
    /// Use the main thread (make sure you are sure if you want to use this!)
    /// </summary>
    MainThread = 1,

    /// <summary>
    /// On some flavours of droid, it is suggested that you must connect on the same thread that you scanned the device
    /// </summary>
    ScanThread = 2
}

```

## Settings

```csharp
// set the thread to connect to the device with
// suggested thread is a method I will attempt to maintain over time
AndroidConfig.ConnectionThread = ConnectionThread.SuggestedConnectionThread; 

// defaults to true.  Background writes on most android devices seem to throw an exception if not connected on the main thread
AndroidConfig.WriteOnMainThread = true; 

// Force Pre-lollipop BLE Scanner - this is more for testing
AndroidConfig.ForcePreLollipopScanner = true;
```