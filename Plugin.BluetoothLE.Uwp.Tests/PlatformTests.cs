using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Xunit;
using GC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE.Uwp.Tests
{
    public class PlatformTests
    {
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

            var serviceResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            var service = serviceResult.Services.First(x => x.Uuid == Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE"));

            var characteristicResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            var characteristic = characteristicResult.Characteristics.First();

            var chtcs = new TaskCompletionSource<byte[]>();
            var handler2 = new TypedEventHandler<GC, GattValueChangedEventArgs>((sender, args) =>
                chtcs.TrySetResult(args.CharacteristicValue.ToArray())
            );
            characteristic.ValueChanged += handler2;
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            var data = await chtcs.Task;

            //characteristic.ValueChanged -= handler2;
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);

            characteristic = null;

            foreach (var s in serviceResult.Services)
            {
                s.Dispose();
            }
            service = null;

            device.Dispose();
            device = null;

            System.GC.Collect();
        }
    }
}
