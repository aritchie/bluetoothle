using System;
using System.Collections.Concurrent;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();


        public IDevice GetDevice(AdvertisementData data)
        {
            return this.devices.GetOrAdd(
                data.BluetoothAddress,
                x => new Device(args)
            );
        }
    }
}