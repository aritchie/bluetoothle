using System;
using System.Collections.Concurrent;
using System.Linq;
using Windows.Devices.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class AdapterContext
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();


        public IDevice AddDevice(BluetoothLEDevice native)
        {
            var dev = new Device(this, native);
            this.devices.TryAdd(native.BluetoothAddress, dev);
            return dev;
        }


        public IDevice GetDevice(ulong bluetoothAddress)
        {
            this.devices.TryGetValue(bluetoothAddress, out var device);
            return device;
        }


        public void Remove(ulong bluetoothAddress) => this.devices.TryRemove(bluetoothAddress, out _);


        public IDevice AddOrGetDevice(BluetoothLEDevice native)
        {
            var dev = this.devices.GetOrAdd(native.BluetoothAddress, id => new Device(this, native));
            return dev;
        }

        //public IEnumerable<IDevice> GetConnectedDevices() => this.devices
        //    .Where(x => x.Value.Status == ConnectionStatus.Connected)
        //    .Select(x => x.Value)
        //    .ToList();


        public void Clear() => this.devices
            .Where(x => x.Value.Status != ConnectionStatus.Connected)
            .ToList()
            .ForEach(x => this.devices.TryRemove(x.Key, out _));
    }
}