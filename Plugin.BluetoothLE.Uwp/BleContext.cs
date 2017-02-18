using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Plugin.BluetoothLE
{
    public class BleContext
    {
        readonly ConcurrentDictionary<ulong, IDevice> devices = new ConcurrentDictionary<ulong, IDevice>();
        const string AqsFilter = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
        static readonly string[] requestProperites = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };


        public IDevice GetDevice(BluetoothLEDevice native)
        {
            return this.devices.GetOrAdd(
                native.BluetoothAddress,
                x => new Device(this, native)
            );
        }


        public IDevice GetDevice(ulong bluetoothAddress)
        {
            IDevice device = null;
            this.devices.TryGetValue(bluetoothAddress, out device);
            return device;
        }


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.devices
                .Where(x => x.Value.Status == ConnectionStatus.Connected)
                .Select(x => x.Value)
                .ToList();
        }


        public void Clear()
        {
            IDevice _;
            this.devices
                .Where(x => x.Value.Status != ConnectionStatus.Connected)
                .ToList()
                .ForEach(x => this.devices.TryRemove(x.Key, out _));
        }


        public IList<IDevice> GetDiscoveredDevices()
        {
            return this.devices.Values.ToList();
        }


        public IObservable<DeviceInformation> CreateDeviceWatcher()
        {
            return Observable.Create<DeviceInformation>(ob =>
            {
                var deviceWatcher = DeviceInformation.CreateWatcher(AqsFilter, requestProperites, DeviceInformationKind.AssociationEndpoint);
                var addHandler = new TypedEventHandler<DeviceWatcher, DeviceInformation>(
                    (sender, args) => ob.OnNext(args)
                );
                //var remHandler = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(
                //    (sender, args) => ob.OnNext(args.)
                //);
                deviceWatcher.Added += addHandler;
                //deviceWatcher.Removed += remHandler;
                //deviceWatcher.EnumerationCompleted += (s, a) =>
                //{
                //    s.Stop();
                //    s.Start();
                //};
                deviceWatcher.Start();

                return () =>
                {
                    deviceWatcher.Stop();
                    deviceWatcher.Added -= addHandler;
                    //deviceWatcher.Removed -= remHandler;
                };
            });
        }


        public IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateAdvertisementWatcher()
        {
            return Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
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
}