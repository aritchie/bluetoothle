using System;
using System.Collections.Concurrent;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();
        readonly IAdapter adapter;


        public DeviceManager(IAdapter adapter)
        {
            this.adapter = adapter;
        }


        public IDevice GetDevice(AdvertisementData data)
        {
            return this.devices.GetOrAdd(
                data.BluetoothAddress,
                x => new Device(this.adapter, data)
            );
        }
    }
}