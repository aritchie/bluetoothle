# Services

**Discover services on a device**

```csharp
// once you have successfully scanned for a device, use the instance to discover services
// NOTE: you can call this repeatedly during the connection lifecycle to see all of the discovered services
Device.WhenServicesDiscovered().Subscribe(service => 
{
});
```