using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


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


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.devices
                .Where(x => x.Value.Status == ConnectionStatus.Connected)
                .Select(x => x.Value)
                .ToList();
        }


        public void Clear()
        {
            IDevice _;
            this.devices
                .Where(x => x.Value.Status != ConnectionStatus.Connected)
                .ToList()
                .ForEach(x => this.devices.TryRemove(x.Key, out _));
        }
    }
}