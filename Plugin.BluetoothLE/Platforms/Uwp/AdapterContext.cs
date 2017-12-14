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
    public class AdapterContext
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


       public IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateAdvertisementWatcher(ScanConfig config)
            => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
            {
                config = config ?? new ScanConfig { ScanType = BleScanType.Balanced };
                var adWatcher = new BluetoothLEAdvertisementWatcher();
                if (config.ServiceUuids != null)
                    foreach (var serviceUuid in config.ServiceUuids)
                        adWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceUuid);

                switch (config.ScanType)
                {
                    case BleScanType.Balanced:
                        adWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                        break;

                    case BleScanType.Background:
                    case BleScanType.LowLatency:
                    case BleScanType.LowPowered:
                        adWatcher.ScanningMode = BluetoothLEScanningMode.Passive;
                        break;
                }
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