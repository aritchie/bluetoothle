# iOS

Apple has some very interesting limitations on Bluetooth especially when it comes to background scanning

## Setup
Make sure to add the following to your Info.plist

```xml
<array>
<string>bluetooth-central</string>
</array>

To add a description to the Bluetooth request message (on iOS 10 this is required!)
```xml
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```

## Backgrounding Information
* Device names are not available at all
* When scanning in the background, pass a ScanConfig argument with a service UUID you wish to scan for.  Without this, you will get nothing

```csharp
BleAdapter.Current.Scan(
    new ScanSettings 
    {
        ServiceUUID = new Guid("<your guid here>")
    }
)
.Subscribe(scanResult => 
{
})
```