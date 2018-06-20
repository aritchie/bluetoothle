using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Xunit;
using Xunit.Abstractions;
using GC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE.Uwp.Tests
{
    public class PlatformTests
    {
        readonly ITestOutputHelper output;


        public PlatformTests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [Fact]
        public async Task ConnectDisconnect()
        {
            var tcs = new TaskCompletionSource<ulong>();
            var adWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                ((sender, args) =>
                {
                    if (args.Advertisement.LocalName.StartsWith("bean", StringComparison.InvariantCultureIgnoreCase))
                        tcs.TrySetResult(args.BluetoothAddress);
                }
            );
            adWatcher.Received += handler;
            adWatcher.Start();

            var bluetoothAddress = await tcs.Task;
            adWatcher.Stop();
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            this.output.WriteLine($"Bluetooth DeviceId: {device.BluetoothDeviceId.Id} - {device.DeviceId} / {device.Name}");

            var serviceResult = await device.GetGattServicesForUuidAsync(Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE"));
            var service = serviceResult.Services.First();
            await service.OpenAsync(GattSharingMode.Exclusive);

            var characteristicResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            var characteristic = characteristicResult.Characteristics.First();

            var chtcs = new TaskCompletionSource<byte[]>();
            var handler2 = new TypedEventHandler<GC, GattValueChangedEventArgs>((sender, args) =>
                chtcs.TrySetResult(args.CharacteristicValue.ToArray())
            );
            characteristic.ValueChanged += handler2;
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            var data = await chtcs.Task;

            // start cleanup
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            characteristic.ValueChanged -= handler2;


            //BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
            //service.Session.SessionStatusChanged += (sender, args) =>
            //{
            //    //args.Status == GattSessionStatus.Active
            //};
            //service.Session.MaintainConnection = true;
            //foreach (var c in characteristicResult.Characteristics)
            //{
            //    c.Service.Session.Dispose();
            //}
            foreach (var s in serviceResult.Services)
            {
                s.Session.Dispose();
            }
            //service = null;

            //var list = device.GetType().GetTypeInfo().GetRuntimeMethods().OrderBy(x => x.Name);

            ////var releasers = list.Where(x => x.Name.StartsWith("Release"));
            ////foreach (var releaser in releasers)
            //foreach (var method in list)
            //{
            //    this.output.WriteLine($"Name: {method.Name}, Static: {method.IsStatic}, Public: {method.IsPublic}, Private: {method.IsPrivate}, Parameters: {method.GetParameters().Length}");
            //}

            service.Dispose();
            service = null;
            characteristic = null;

            device.Dispose();
            device = null;

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }


        async Task Setup()
        {
            var native = await BluetoothAdapter.GetDefaultAsync();
            var radio = await native.GetRadioAsync();
            CrossBleAdapter.Current = new Adapter(native, radio);
        }

        [Fact]
        public async Task GetPairedDevicesTest()
        {
            await this.Setup();

            var ad = CrossBleAdapter.Current;
            var devices = ad.GetPairedDevices();

            foreach (var device in devices)
            {
                this.output.WriteLine($"Paired Bluetooth Devices: Name={device.Name} UUID={device.Uuid} Paired={device.PairingStatus}");
                Assert.True(device.PairingStatus == PairingStatus.Paired);
            }
        }
        [Fact]
        public async Task GetConnectedDevicesTest()
        {
            await this.Setup();

            var ad = CrossBleAdapter.Current;
            var devices = ad.GetConnectedDevices();

            if (devices.Count() == 0)
            {
                this.output.WriteLine($"There are no connected Bluetooth devices. Trying to connect a device...");
                var paired = ad.GetPairedDevices();

                // Get the first paired device
                var device = paired.FirstOrDefault();
                if (device != null)
                {
                    await device.ConnectWait().ToTask();
                    devices = ad.GetConnectedDevices();
                }
                else
                {
                    this.output.WriteLine($"There are no connected Bluetooth devices. Connect a device and try again.");
                }
            }
        
            foreach (var device in devices)
            {
                this.output.WriteLine($"Connected Bluetooth Devices: Name={device.Name} UUID={device.Uuid} Connected={device.IsConnected()}");
                Assert.True(device.Status == ConnectionStatus.Connected);
            }
        }
        [Fact]
        public async Task GetKnownDeviceTest()
        {
            await this.Setup();

            var ad = CrossBleAdapter.Current;
            var devices = ad.GetPairedDevices();

            // Get the first paired device
            var known = devices.FirstOrDefault();
            if (known != null)
            {
                // Now try to get it from the known Devices
                var found = ad.GetKnownDevice(known.Uuid);
                Assert.True(known.Uuid == found.Uuid);
            }
            else
            {
                this.output.WriteLine($"No well known device found to test with. Please pair a device");
            }
        }
        [Fact]
        public async Task CancelConnectionTest()
        {
            await this.Setup();

            var ad = CrossBleAdapter.Current;
            var devices = ad.GetPairedDevices();

            // Get the first paired device
            var known = devices.FirstOrDefault();
            if (known != null)
            {
                Guid knownUuid = known.Uuid;
                // Make sure that is not connected already
                var device = ad.GetConnectedDevices().FirstOrDefault(d => d.Uuid == knownUuid);
                Assert.Null(device);

                // Connect to it
                await known.ConnectWait().ToTask();

                device = ad.GetConnectedDevices().FirstOrDefault(d => d.Uuid == knownUuid);
                // Make sure it is still connected
                Assert.NotNull(device);
                
                // Disconnect
                known.CancelConnection();
                // and also we have to dispose of (not to hold a reference of) to the device returned from GetConnectedDevices() else GC will not collect it and it will not disconnect on Dispose.
                // As long as there is a reference to a device (device instance AS A SINGLETON) it will not disconnect. Even if we don't explicitly open a connection the second device (third,... reference to the device)
                // can still read and write to the BT it doesn't have to explicitly connect if it is already connected. So any BT Device instantiation like FromIdAsync, FromBluetoothAddressAsync and in general any FromXXXX
                // will return the same BT with or without a connection. 
                device.CancelConnection(); // to actually only call NativeDevice.Dispose(). // We can make the IDevice : IDisposable to simplify?
                
                // Give it a couple of seconds to disconnect
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                device = ad.GetConnectedDevices().FirstOrDefault(d => d.Uuid == knownUuid);
                // Make sure it is not connected
                Assert.Null(device); 
            }
        }
    }
}
