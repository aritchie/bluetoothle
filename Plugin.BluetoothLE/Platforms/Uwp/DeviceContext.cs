using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Acr.Logging;
using NC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public class DeviceContext
    {
        readonly object syncLock;
        readonly AdapterContext adapterContext;
        readonly IList<NC> subscribers;


        public DeviceContext(AdapterContext adapterContext,
                             IDevice device,
                             BluetoothLEDevice native)
        {
            this.syncLock = new object();
            this.adapterContext = adapterContext;
            this.subscribers = new List<NC>();
            this.Device = device;
            this.NativeDevice = native;
        }


        public IDevice Device { get; }
        public BluetoothLEDevice NativeDevice { get; set; }


        public async Task Disconnect()
        {
            if (this.NativeDevice == null)
                return;

            foreach (var ch in this.subscribers)
            {
                try
                {
                    await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                }
                catch (Exception e)
                {
                    Log.Info(BleLogCategory.Device, "Disconnect Error - " + e);
                }
            }
            this.subscribers.Clear();
            this.adapterContext.Remove(this.NativeDevice.BluetoothAddress);

            this.NativeDevice?.Dispose();
            this.NativeDevice = null;
            GC.Collect();
        }


        public void SetNotifyCharacteristic(NC characteristic, bool enable)
        {
            lock (this.syncLock)
            {
                if (enable)
                {
                    this.subscribers.Add(characteristic);
                }
                else
                {
                    this.subscribers.Remove(characteristic);
                }
            }
        }
    }
}