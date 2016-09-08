using System;
using System.Collections.Concurrent;
using Windows.Devices.Bluetooth.Advertisement;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();


        public IDevice GetDevice(BluetoothLEAdvertisementReceivedEventArgs deviceInfo)
        {
            return this.devices.GetOrAdd(
                deviceInfo.BluetoothAddress,
                x => new Device(deviceInfo.BluetoothAddress, deviceInfo.Advertisement.LocalName)
            );
        }
    }
}
