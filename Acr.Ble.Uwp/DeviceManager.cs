using System;
using System.Collections.Concurrent;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();


        public bool TryGetDevice(ulong bluetoothAddress, out IDevice device)
        {
            return this.devices.TryGetValue(bluetoothAddress, out device);
        }


        public IDevice GetDevice(GattDeviceService native)
        {
            return this.devices.GetOrAdd(
                native.Device.BluetoothAddress,
                x => new Device(native)
            );
        }
    }
}