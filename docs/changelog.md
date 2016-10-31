# CHANGE LOG

1.0.4
[fix] logging cleanup
[feature][core] add DiscoveredServices, DiscoveredCharacteristics, and DiscoveredDescriptors for easy access
[feature][core] add logging abilities from device reference
[feature][droid] add improved way to deal with Android connection issues (please read docs under Android Troubleshooting)

1.0.3
[fix][core] logging would not hook properly to existing connected devices
[fix][droid] deal with gatt error 133 by delaying service discovery post connection
[workaround] tvOS was having issues. temporarily pulled from nuget

1.0.2
[feature] write without response void method added
[feature] proper equals check for all ble objects

1.0.1
[fix][all] new adapter scans only clear disconnected devices from cache
[feature] Adapter.GetConnectedDevices

1.0.0
[fix][droid] WhenStatusChanged firing on subscription and replays properly
[fix][droid] properly parsing 16 and 32bit UUIDs in advertisement packet

0.9.9
[breaking] WhenActionOccurs renamed to CreateLogger
[fix] ensure WhenScanStatusChanged() broadcasts its current state on registration
Logging now returns actual packet received where applicable

0.9.8
adding tvOS libraries to package (NOT TESTED)
[fix] createconnection properly persists connection now
[fix] more logging and discovery issues
[fix][droid] device.readrssi was not working
[droid] device.whenstatuschanged will now broadcast Connecting/Disconnecting
[droid] advertisement packet now gets all service UUIDs parsed

0.9.7
[fix] Error notifications on read/writes
[fix] Make sure to replay last status for connectable observables
[fix] Service discovery on iOS and Android was not registering subsequent subscriptions properly
[fix][droid] Read/Write callbacks now passing values back properly
[breaking] PersistentConnection is now CreateConnection with improvements to status reporting

0.9.6
Vastly improved logging
Improvements to observable allocations
Improvements in service discovery

0.9.5
[breaking] Change extension method names

0.9.4
[breaking] Characteristic method WhenNotificationOccurs() is now called WhenNotificationReceived().  It also no longer subscribes to notifications.  Use new method SubscribeToNotifications().  WhenNotificationReceived() is for logging purposes

0.9.3
Add heartrate plugin (extension method)
Add super logging plugin (extension method)
Characteristics and Descriptors now have WhenRead/WhenWritten events to monitor calls externally

0.9.2
ScanListen for working with scan results from a background or decoupled component

0.9.1
BackgroundScan added and ScanFilter removed
Multiple entry points can now hook up to scan, but only one will run (connectable refcount observable)

0.9.0
Initial Public Release