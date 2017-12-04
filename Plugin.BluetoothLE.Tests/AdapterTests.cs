using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class AdapterTests : AbstractTests
    {

        public AdapterTests(ITestOutputHelper output) : base(output)
        {
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

            on.Should().BeGreaterOrEqualTo(1);
            off.Should().BeGreaterOrEqualTo(1);
        }


        [Fact]
        public async Task ScanFilter()
        {
            var result = await CrossBleAdapter
                .Current
                .Scan(new ScanConfig
                {
                    ScanType = BleScanType.Balanced,
                    ServiceUuids =
                    {
                        ScratchServiceUuid
                    }
                })
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(20))
                .ToTask();

            result.Should().NotBeNull("Device not found");
            result.Device.Name.Should().Be("Bean+");
        }
    }
}
