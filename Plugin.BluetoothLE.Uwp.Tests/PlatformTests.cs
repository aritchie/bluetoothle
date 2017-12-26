using System;
using System.Linq;
using System.Reflection;
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
            characteristic = null;

            service.Dispose();
            //BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
            //service.Session.SessionStatusChanged += (sender, args) =>
            //{
            //    //args.Status == GattSessionStatus.Active
            //};
            //service.Session.MaintainConnection = true;
            //foreach (var s in serviceResult.Services)
            //{
            //    s.Session.MaintainConnection = false;
            //    s.Session.Dispose();
            //    s.Dispose();
            //}
            //service = null;

            var list = device.GetType().GetTypeInfo().GetRuntimeMethods().OrderBy(x => x.Name);

            //var releasers = list.Where(x => x.Name.StartsWith("Release"));
            //foreach (var releaser in releasers)
            foreach (var method in list)
            {
                this.output.WriteLine($"Name: {method.Name}, Static: {method.IsStatic}, Public: {method.IsPublic}, Private: {method.IsPrivate}, Parameters: {method.GetParameters().Length}");
            }

            device.Dispose();
            device = null;

            System.GC.Collect();
        }
    }
}
