using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class AdapterTests
    {
        readonly ITestOutputHelper output;


        public AdapterTests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [Fact]
        public async Task Status_Monitor()
        {
            var on = 0;
            var off = 0;
            CrossBleAdapter
                .Current
                .WhenStatusChanged()
                .Skip(1) // skip startwith
                .Subscribe(x =>
                {
                    switch (x)
                    {
                        case AdapterStatus.PoweredOn:
                            on++;
                            break;

                        case AdapterStatus.PoweredOff:
                            off++;
                            break;
                    }
                });
            await UserDialogs.Instance.AlertAsync("Now turn the adapter off and then back on - press ok once done");

            Assert.True(on >= 1);
            Assert.True(off >= 1);
        }


        [Fact]
        public async Task Scan_Filter()
        {
            var result = await CrossBleAdapter
                .Current
                .Scan(new ScanConfig
                {
                    ScanType = BleScanType.Balanced,
                    ServiceUuids =
                    {
                        Constants.AdServiceUuid
                    }
                })
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(20))
                .ToTask();

            Assert.NotNull(result);
            Assert.Equal("Bean+", result.Device.Name);
        }


        [Fact]
        public async Task Scan_Extra_BackToBack()
        {
            var ad = CrossBleAdapter.Current;

            var sub = ad.ScanExtra().Subscribe();

            Assert.True(CrossBleAdapter.Current.IsScanning);
            //await Task.Delay(2000);
            sub.Dispose();

            Assert.False(CrossBleAdapter.Current.IsScanning);
            sub = ad.ScanExtra(restart: true).Subscribe();
            Assert.True(CrossBleAdapter.Current.IsScanning);

            sub.Dispose();
        }


        [Fact]
        public async Task Devices_GetPaired()
        {
            var ad = CrossBleAdapter.Current;
            var devices = await ad.GetPairedDevices();

            foreach (var device in devices)
            {
                this.output.WriteLine($"Paired Bluetooth Devices: Name={device.Name} UUID={device.Uuid} Paired={device.PairingStatus}");
                Assert.True(device.PairingStatus == PairingStatus.Paired);
            }
        }


        [Fact]
        public async Task Devices_GetConnected()
        {
            var ad = CrossBleAdapter.Current;
            var devices = await ad.GetConnectedDevices();

            if (devices.Count() == 0)
            {
                this.output.WriteLine($"There are no connected Bluetooth devices. Trying to connect a device...");
                var paired = await ad.GetPairedDevices();

                // Get the first paired device
                var device = paired.FirstOrDefault();
                if (device != null)
                {
                    await device.ConnectWait().ToTask();
                    devices = await ad.GetConnectedDevices();
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
        public async Task Devices_GetKnown()
        {
            var ad = CrossBleAdapter.Current;
            var devices = await ad.GetPairedDevices();

            // Get the first paired device
            var known = devices.FirstOrDefault();
            if (known != null)
            {
                // Now try to get it from the known Devices
                var found = await ad.GetKnownDevice(known.Uuid);
                Assert.True(known.Uuid == found.Uuid);
            }
            else
            {
                this.output.WriteLine($"No well known device found to test with. Please pair a device");
            }
        }
    }
}
