using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Xunit;


namespace Plugin.BluetoothLE.Tests
{
    public class AdapterTests
    {
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
            await Task.Delay(2000);
            sub.Dispose();

            Assert.False(CrossBleAdapter.Current.IsScanning);
            sub = ad.ScanExtra(restart: true).Subscribe();
            Assert.True(CrossBleAdapter.Current.IsScanning);

            sub.Dispose();
        }
    }
}
