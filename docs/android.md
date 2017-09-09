# Android

Android Bluetooth is painful and that's being nice.  This library attempts to deal with the necessary thread handling all internally.

All of the classes and members listed in this page can only be called from your Android project, not your PCL/Core library.  You should call and set
these values at your main launcher activity or even at an application level.
```

## Settings

```csharp
// returns a suggestion on what thread to execute on.  This is not used internally
AndroidConfig.MainThreadSuggested { get; }

// defaults to MainThreadSuggested.  Background actions most android devices seem to throw an exception if not connected on the main thread
AndroidConfig.PerformActionsOnMainThread = true; 
```