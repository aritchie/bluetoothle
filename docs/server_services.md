#Services

Services are nothing more than categories in the overall perspective of BluetoothLE.  You should be aware that
you must setup all characters & descriptors that belong to a service BEFORE starting advertising or adding a service
to a running server!

You should always know your service UUID for future client consumption!

From a functionality perspective, there is not a lot you do with services

## Setup

```csharp
var server = CrossBleAdapter.Current.CreateGattServer();

// first parameter is your controlled GUID/UUID
// second parameter specifies if it is the primary service
server.CreateService(Guid.NewGuid(), true);

```