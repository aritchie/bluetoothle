using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Android.Bluetooth;


namespace Acr.Ble.Internals
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> devices = new ConcurrentDictionary<string, IDevice>();
        readonly BluetoothManager manager;
        readonly GattCallbacks callbacks;


        public DeviceManager(BluetoothManager manager, GattCallbacks callbacks)
        {
            this.manager = manager;
            this.callbacks = callbacks;
        }


        public IDevice GetDevice(BluetoothDevice btDevice, TaskScheduler scheduler)
        {
            return this.devices.GetOrAdd(
                btDevice.Address,
                x => new Device(this.manager, btDevice, this.callbacks, scheduler)
            );
        }


        public void Clear()
        {
            this.devices.Clear();
        }
    }
}