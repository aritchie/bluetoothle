using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth;


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


        public IDevice GetDevice(BluetoothLEDevice native)
        {
            return this.devices.GetOrAdd(
                native.BluetoothAddress,
                x => new Device(this.adapter, native)
            );
        }


        public IDevice GetDevice(ulong bluetoothAddress)
        {
            IDevice device = null;
            this.devices.TryGetValue(bluetoothAddress, out device);
            return device;
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


        public IList<IDevice> GetDiscoveredDevices()
        {
            return this.devices.Values.ToList();
        }
    }
}