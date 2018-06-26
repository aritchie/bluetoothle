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


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            var nativeDevices = this.manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, new[]
            {
                (int) ProfileState.Connecting,
                (int) ProfileState.Connected
            });
            foreach (var native in nativeDevices)
                yield return this.GetDevice(native);
        }


        public void Clear()
        {
            var connectedDevices = this.GetConnectedDevices().ToList();
            this.devices.Clear();
            foreach (var dev in connectedDevices)
                this.devices.TryAdd(((BluetoothDevice) dev.NativeDevice).Address, dev);
        }
    }
}