using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.UI.Core;


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


        public async Task Disconnect(BluetoothLEDevice native)
        {
            this.devices.TryRemove(native.BluetoothAddress, out _);
            var ns = await native.GetGattServicesAsync(BluetoothCacheMode.Cached);
            foreach (var nservice in ns.Services)
            {
                var nch = await nservice.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
                var tcs = new TaskCompletionSource<object>();
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(
                    CoreDispatcherPriority.High,
                    async () =>
                    {
                        foreach (var characteristic in nch.Characteristics)
                        {
                            if (!characteristic.HasNotify())
                                return;

                            try
                            {
                                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                            }
                            catch (Exception e)
                            {
                                //System.Console.WriteLine(e);
                                System.Diagnostics.Debug.WriteLine(e.ToString());
                            }
                        }
                        tcs.TrySetResult(null);
                    }
                );
                await tcs.Task;
                nservice.Dispose();
            }
            native.Dispose();
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