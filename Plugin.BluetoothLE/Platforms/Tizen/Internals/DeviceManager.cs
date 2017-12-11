using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> devices = new ConcurrentDictionary<string, IDevice>();


        public IDevice GetDevice(BluetoothLeDevice btDevice) => this.devices.GetOrAdd(
            btDevice.RemoteAddress,
            x => new Device(btDevice)
        );

        public IEnumerable<IDevice> GetConnectedDevices() => this.devices
            .Where(x => x.Value.Status == ConnectionStatus.Connected)
            .Select(x => x.Value)
            .ToList();

        public void Clear() => this.devices
            .Where(x => x.Value.Status != ConnectionStatus.Connected)
            .ToList()
            .ForEach(x => this.devices.TryRemove(x.Key, out _));
    }
}