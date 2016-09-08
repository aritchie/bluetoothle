using System;
using System.Collections.Concurrent;
using CoreBluetooth;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> peripherals = new ConcurrentDictionary<string, IDevice>();
        readonly CBCentralManager manager;


        public DeviceManager(CBCentralManager manager)
        {
            this.manager = manager;
        }


        public IDevice GetDevice(CBPeripheral peripheral)
        {
            return this.peripherals.GetOrAdd(
                peripheral.Identifier.ToString(),
                x => new Device(this.manager, peripheral)
            );
        }


        public void Clear() 
        {
            this.peripherals.Clear();
        }
    }
}
