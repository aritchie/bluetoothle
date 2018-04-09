using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> devices = new ConcurrentDictionary<string, IDevice>();
        readonly BluetoothManager manager;


        public DeviceManager(BluetoothManager manager)
        {
            this.manager = manager;
        }


        public IDevice GetDevice(BluetoothDevice btDevice) => this.devices.GetOrAdd(
            btDevice.Address,
            x => new Device(this.manager, btDevice)
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