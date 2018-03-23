# CHANGE LOG

## 5.3.5
* [fix][android] characteristic disconnect crash

## 5.3.4
* The locking mechanism for android has been turned off by default

## 5.3.3
* [fix][android] removes lock on android getknowncharacteristic - this could introduce android bug back into wild
* [fix][android] more deadlock issues


## 5.3.2
* [fix][android] fixes to locking mechanism as well as ability to disable it via CrossBleAdapter.AndroidDisableLockMechanism
* [fix][ios] NRE in reconnection logic

## 5.3.1
* [fix][android] GetKnownCharacteristic now sync locks to prevent android race condition
* [feature][android] configurable parameter to allow "breather" time between operations to prevent GATT issues

## 5.3
* [feature][android] Advertisement service UUID filtering for Pre-Lollipop
* [fix][android] Fix issue with stopping scan when bluetooth adapter becomes disabled
* [fix][android] Fix NRE with reconnection WhenServiceDiscovered
* [fix][android] More improvements to race conditions
* [BREAKING] Adapter.ScanListen has been removed

## 5.2.2
* [feature] push .NET standard 2.0
* [feature] push to android 8 (forced nuget compile target - Android 4.3+ is still supported)
* [fix][android] fix race conditions around semaphore cleanup

## 5.2
* [fix][android] more connection fixes to alleviate GATT 133
* [fix][android] advertisement service UUIDs not parsing properly

## 5.1
* [fix][android] rewritten connect/disconnect logic
* [fix][android][ios] rewritten reconnection logic
* [fix][android] kill more gatt133 errors by forcing synchronized communication (hidden to consumer)
* [feature][android] Ability to use Android AutoConnect option on connect
* [feature] scan for multiple UUIDs

## 5.0
* [breaking][feature] SetNotificationValue has been replaced with EnableNotifications/DisableNotifications.
* [fix][ios] NRE when read/notification value is null
* [fix][android] service UUIDs in advertisement not being parsed correctly
* [fix][android] cleanup internal thread delegation
* [fix][uwp] don't marshall to main thread for most calls
* [fix][uwp] mac address length

## 4.0.1
* [fix][ios] NRE race condition in GetKnownCharacteristic

## 4.0
* [feature] .net standard support
* [feature][breaking] characteristics must now have their notifications enabled/disabled using characteristic.SetNotificationValue(..);
* [uwp] Connection status and general connection keep-alive improvements

## 3.1
* [feature] UWP client beta (server support has not been tested)
* [feature] Adapter scanner now has an empty implementation for platforms where it is not supported


## 3.0
* [feature] GATT Server is now built into this library
* [feature] Manufacturer data can be advertised on Windows and Android
* [feature] ability to scan for multiple bluetooth adapters
* [feature] expose service data as part of advertisement data
* [feature] expose native device from IDevice as object
* [feature] New methods - Device.GetKnownService, Service.GetKnownCharacteristics(uuids), and Device.GetKnownCharacteristics(serviceUuid, characteristicUuids)
* [fix][android] GetKnownDevice
* [fix][android] bad UUID parsing in ad data for service UUIDs
* [fix][android] multiple notification subscriptions
* [fix][ios] reconnection issues

## 2.0.3
* [fix][android][ios] improved equality checks to help with android events

## 2.0.2
* [fix][android] more gatt133 fixes
* [fix][android] additional fixes for cancel connection
* [fix][android] Connect completion wasn't being called properly

## 2.0.1
* [fix][android] finalization was causing NRE

## 2.0
* [feature] macOS support!
* [feature][all] Connection configuration allows you to set connection priority, notification states on iOS/tvOS/macOS, and whether or not to make the connection persistent
* [feature][macos/tvos/ios] Background mode via CBCentralInitOptions - On the platform project use BleAdapter.Init(BleAdapterConfiguration)
* [feature][ios] Background - Adapter.WhenDeviceStateRestored() will allow to hook for background state restoration (must be used in conjunction with BleAdapter.Init)
* [feature][uwp][droid] Reliable write transaction via Device.BeginReliableWriteTransaction() and GattReliableWriteTransaction
* [feature][uwp][droid] WriteBlob now uses reliable write transactions
* [feature] Device.GetService(Guid[]) and Service.GetCharacteristic(Guid[]) optimized calls
* [feature] Adapter.GetKnownDevice(Guid) - explanation in the signature :)
* [feature] Adapter.GetPairedDevices() - pretty self explanatory
* [breaking][feature] RequestMtu now returns as an observable with what the accepted MTU was
* [breaking] CreateConnection is gone - created more issues than it solved - Use Connect() as it creates persistent connections out of the gate
* [breaking] Disconnect has been renamed to CancelConnection as it cancels any pending connections now
* [breaking] BleAdapter has been renamed to CrossBleAdapter
* [breaking] Acr.Ble namespace has been renamed to Plugin.BluetoothLE
* [fix][droid] disconnect on existing connection tries
* [fix][droid] more gatt 133 issues
* [fix][all] Blob write observable subscriptions not firing properly
* [fix][all] NotifyEncryptionRequired, Indicate, and IndicateEncryptionRequired return true for CanNotify

## 1.3
* [fix][droid] descriptors and characteristic read/writes now adhere to AndroidConfig.WriteOnMainThread
* [fix][ios] WhenStatusChanged was causing OnError when a connection failure occurred
* [fix][core] BlobWrite will now use proper MTU
* [breaking][feature][core] Background scan has been replaced.  The normal scan now takes a configuration.
* [feature][core] Get current MTU size
* [feature][droid] monitor MTU changes

## 1.2
* [feature] ability to open bluetooth settings configuration
* [feature] ability to request MTU is now part of device (still only available on droid - but allows for greater flexibility)
* [feature][droid] ability to pair with a device
* [feature][droid] ability to toggle bluetooth adapter status

## 1.1
* [BREAKING] Characteristic/Descriptor Read, Write, and Notification events now return CharacteristicResult that includes the sender characteristic as well as the data
* [fix][droid] Write was not broadcasting completion at the right time

## 1.0.8
[fix] proper completion of ReadUntil

## 1.0.7
* [feature] IGattCharacteristic.ReadUntil(endBytes) extension method will read in a loop until end bytes detected
* [feature][droid] AndroidConfig.MaxTransmissionUnitSize (MTU) can now be set to negotiate MTU upon connections

## 1.0.6
* [fix][droid] write on main thread (can use AndroidConfig.WriteOnMainThread = false, to disable)
* [feature] Blob write
* [feature] Logging now has deviceconnected/devicedisconnected if you wish to monitor just one of the status'

## 1.0.5
* [fix] ability to check for true WriteNoResponse flags
* [fix][droid] ship proper unsubscribe bytes

## 1.0.4
* [fix] logging cleanup
* [feature][core] add DiscoveredServices, DiscoveredCharacteristics, and DiscoveredDescriptors for easy access
* [feature][core] add logging abilities from device reference
* [feature][droid] add improved way to deal with Android connection issues (please read docs under Android Troubleshooting)

## 1.0.3
* [fix][core] logging would not hook properly to existing connected devices
* [fix][droid] deal with gatt error 133 by delaying service discovery post connection
* [workaround] tvOS was having issues. temporarily pulled from nuget

## 1.0.2
* [feature] write without response void method added
* [feature] proper equals check for all ble objects

## 1.0.1
* [fix][all] new adapter scans only clear disconnected devices from cache
* [feature] Adapter.GetConnectedDevices

## 1.0.0
* [fix][droid] WhenStatusChanged firing on subscription and replays properly
* [fix][droid] properly parsing 16 and 32bit UUIDs in advertisement packet

## 0.9.9
*[breaking] WhenActionOccurs renamed to CreateLogger
*[fix] ensure WhenScanStatusChanged() broadcasts its current state on registration
*Logging now returns actual packet received where applicable

## 0.9.8
* adding tvOS libraries to package (NOT TESTED)
* [fix] createconnection properly persists connection now
* [fix] more logging and discovery issues
* [fix][droid] device.readrssi was not working
* [droid] device.whenstatuschanged will now broadcast Connecting/Disconnecting
* [droid] advertisement packet now gets all service UUIDs parsed

## 0.9.7
* [fix] Error notifications on read/writes
* [fix] Make sure to replay last status for connectable observables
* [fix] Service discovery on iOS and Android was not registering subsequent subscriptions properly
* [fix][droid] Read/Write callbacks now passing values back properly
* [breaking] PersistentConnection is now CreateConnection with improvements to status reporting

## 0.9.6
* Vastly improved logging
* Improvements to observable allocations
* Improvements in service discovery

## 0.9.5
* [breaking] Change extension method names

## 0.9.4
* [breaking] Characteristic method WhenNotificationOccurs() is now called WhenNotificationReceived().  It also no longer subscribes to notifications.  Use new method SubscribeToNotifications().  WhenNotificationReceived() is for logging purposes

## 0.9.3
* Add heartrate plugin (extension method)
* Add super logging plugin (extension method)
* Characteristics and Descriptors now have WhenRead/WhenWritten events to monitor calls externally

## 0.9.2
* ScanListen for working with scan results from a background or decoupled component

## 0.9.1
* BackgroundScan added and ScanFilter removed
* Multiple entry points can now hook up to scan, but only one will run (connectable refcount observable)

## 0.9.0
* Initial Public Release
