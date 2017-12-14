using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Core;
using NC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public class DeviceContext
    {
        readonly object syncLock;
        readonly IList<NC> subscribers;
        IDisposable keepAlive;


        public DeviceContext(IDevice device, BluetoothLEDevice native)
        {
            this.syncLock = new object();
            this.subscribers = new List<NC>();
            this.Device = device;
            this.NativeDevice = native;
        }


        public IDevice Device { get; }
        public BluetoothLEDevice NativeDevice { get; }


        void StartKeepAlive()
        {
            if (this.keepAlive != null)
                return;

            this.keepAlive = Observable
                .Interval(TimeSpan.FromSeconds(5))
                .Subscribe(_ => this.Ping());
        }


        void StopKeepAlive()
        {
            this.keepAlive?.Dispose();
            this.keepAlive = null;
        }


        public void Ping() => this.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached); // fire and forget
        public void Connect() => this.StartKeepAlive();


        public async Task Disconnect()
        {
            this.StopKeepAlive();

            foreach (var ch in this.subscribers)
            {
                try
                {
                    await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
                this.subscribers.Clear();
            }
            var result = await this.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
            foreach (var s in result.Services)
            {
                s.Dispose();
            }
            GC.Collect();
            //this.NativeDevice.Dispose();
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

                if (this.subscribers.Any())
                {
                    this.StopKeepAlive();
                }
                else
                {
                    this.StartKeepAlive();
                }
            }
        }
    }
}