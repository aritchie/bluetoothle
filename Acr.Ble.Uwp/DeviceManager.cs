using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Core;


namespace Acr.Ble
{
    public class DeviceManager
    {
        readonly IDictionary<ulong, IDevice> devices = new Dictionary<ulong, IDevice>();
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(Int32.MaxValue);


        public async Task<IDevice> GetDevice(BluetoothLEAdvertisementReceivedEventArgs deviceInfo)
        {
            if (!this.devices.ContainsKey(deviceInfo.BluetoothAddress))
            {
                await this.semaphore.WaitAsync();
                if (!this.devices.ContainsKey(deviceInfo.BluetoothAddress))
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                    {
                        var deviceId = BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(deviceInfo.BluetoothAddress);
                        
                        var native = await GattDeviceService.FromIdAsync(deviceId);
                        var device = new Device(native);
                        this.devices.Add(deviceInfo.BluetoothAddress, device);
                    });
                }
                this.semaphore.Release();
            }
            return this.devices[deviceInfo.BluetoothAddress];
        }
    }
}
/*
private void SetupBluetooth()
    {
        Watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        Watcher.Received += DeviceFound;

        DeviceWatcher = DeviceInformation.CreateWatcher();
        DeviceWatcher.Added += DeviceAdded;
        DeviceWatcher.Updated += DeviceUpdated;

        StartScanning();
    }

    private void StartScanning()
    {
        Watcher.Start();
        DeviceWatcher.Start();
    }

    private void StopScanning()
    {
        Watcher.Stop();
        DeviceWatcher.Stop();
    }

    private async void DeviceFound(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs btAdv)
    {
        if (_devices.Contains(btAdv.Advertisement.LocalName))
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                Debug.WriteLine($"---------------------- {btAdv.Advertisement.LocalName} ----------------------");
                Debug.WriteLine($"Advertisement Data: {btAdv.Advertisement.ServiceUuids.Count}");
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                var result = await device.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                Debug.WriteLine($"Pairing Result: {result.Status}");
                Debug.WriteLine($"Connected Data: {device.GattServices.Count}");
            });
        }
    }

    private async void DeviceAdded(DeviceWatcher watcher, DeviceInformation device)
    {
        if (_devices.Contains(device.Name))
        {
            try
            {
                var service = await GattDeviceService.FromIdAsync(device.Id);
                Debug.WriteLine("Opened Service!!");
            }
            catch
            {
                Debug.WriteLine("Failed to open service.");
            }
        }
    }

    private void DeviceUpdated(DeviceWatcher watcher, DeviceInformationUpdate update)
    {
        Debug.WriteLine($"Device updated: {update.Id}");
    }
     */