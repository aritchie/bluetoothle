using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.peripherals
                .Where(x => x.Value.Status == ConnectionStatus.Connected)
                .Select(x => x.Value)
                .ToList();
        }


        public void Clear()
        {
            IDevice _;
            this.peripherals
                .Where(x => x.Value.Status != ConnectionStatus.Connected)
                .ToList()
                .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
        }
    }
}
