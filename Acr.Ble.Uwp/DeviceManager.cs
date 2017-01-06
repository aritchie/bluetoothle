using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> devices = new ConcurrentDictionary<string, IDevice>();
        readonly IAdapter adapter;


        public DeviceManager(IAdapter adapter)
        {
            this.adapter = adapter;
        }


        public IDevice GetDevice(DeviceInformation deviceInfo)
        {
            return this.devices.GetOrAdd(
                deviceInfo.Id,
                x => new Device(this.adapter, deviceInfo)
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