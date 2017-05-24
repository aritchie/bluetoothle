using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;


namespace Plugin.BluetoothLE
{
    public class BleContext
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();


        public IDevice AddDevice(ulong bluetoothAddress, BluetoothLEDevice native)
        {
            var dev = new Device(this, native);
            this.devices.TryAdd(bluetoothAddress, dev);
            return dev;
        }


        public IDevice GetDevice(ulong bluetoothAddress)
        {
            this.devices.TryGetValue(bluetoothAddress, out var device);
            return device;
        }


        public IEnumerable<IDevice> GetConnectedDevices() => this.devices
            .Where(x => x.Value.Status == ConnectionStatus.Connected)
            .Select(x => x.Value)
            .ToList();


        public void Clear() => this.devices
            .Where(x => x.Value.Status != ConnectionStatus.Connected)
            .ToList()
            .ForEach(x => this.devices.TryRemove(x.Key, out _));


        public IList<IDevice> GetDiscoveredDevices() => this.devices.Values.ToList();


       public IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateAdvertisementWatcher()
            => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
            {
                var adWatcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };
                var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                    ((sender, args) => ob.OnNext(args)
                );
                adWatcher.Received += handler;
                adWatcher.Start();

                return () =>
                {
                    adWatcher.Stop();
                    adWatcher.Received -= handler;
                };
            });
    }
}