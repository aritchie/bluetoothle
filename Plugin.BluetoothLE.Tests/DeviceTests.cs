using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class DeviceTests : AbstractTests
    {
        readonly ITestOutputHelper output;


        public DeviceTests(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public async Task WhenServiceFound_Reconnect_ShouldFlushOriginals()
        {
            var autoConnect = await UserDialogs.Instance.ConfirmAsync(new ConfirmConfig().SetMessage("Use autoConnect?").UseYesNo());

            var count = 0;
            await this.FindTestDevice();
            this.Device.WhenServiceDiscovered().Subscribe(_ => count++);

            await this.Device.Connect(new GattConnectionConfig
            {
                AutoConnect = autoConnect,
                IsPersistent = true
            });
            await UserDialogs.Instance.AlertAsync("Now turn device off & press OK");
            var origCount = count;
            count = 0;

            await UserDialogs.Instance.AlertAsync("No turn device back on & press OK when light turns green");
            count.Should().Be(origCount);
        }


        [Fact]
        public async Task GetKnownServicesConsecutively()
        {
            await this.FindTestDevice();
            await this.Device.Connect();
            var s1 = await this.Device
                .GetKnownCharacteristics(ScratchServiceUuid, new Guid("A495FF21-C5B1-4B44-B512-1370F02D74DE"))
                .Timeout(TimeSpan.FromSeconds(5));

            var s2 = await this.Device
                .GetKnownCharacteristics(ScratchServiceUuid, new Guid("A495FF22-C5B1-4B44-B512-1370F02D74DE"))
                .Timeout(TimeSpan.FromSeconds(5));

            s1.Should().NotBeNull();
            s2.Should().NotBeNull();
        }


        [Fact]
        public async Task ReconnectTest()
        {
            var autoConnect = await UserDialogs.Instance.ConfirmAsync(new ConfirmConfig().SetMessage("Use autoConnect?").UseYesNo());
            var connected = 0;
            var disconnected = 0;

            await this.FindTestDevice();
            this.Device
                .WhenStatusChanged()
                .Subscribe(x =>
                {
                    switch (x)
                    {
                        case ConnectionStatus.Disconnected:
                            disconnected++;
                            break;

                        case ConnectionStatus.Connected:
                            connected++;
                            break;
                    }
                });

            await this.Device.Connect(new GattConnectionConfig
            {
                AutoConnect = autoConnect,
                IsPersistent = true
            });
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            connected.Should().Be(2, "No reconnection count");
            disconnected.Should().Be(2, "No disconnect");
        }



        /*
        Service (ad-data) - A495FF20-C5B1-4B44-B512-1370F02D74DE

        // start count
        Scratch 1 - A495FF21-C5B1-4B44-B512-1370F02D74DE

        // temp
        Scratch 2 - A495FF22-C5B1-4B44-B512-1370F02D74DE

        // accel X
        Scratch 3 - A495FF23-C5B1-4B44-B512-1370F02D74DE

        // accel Y
        Scratch 4 - A495FF24-C5B1-4B44-B512-1370F02D74DE

        // accel Z
        Scratch 5 - A495FF25-C5B1-4B44-B512-1370F02D74DE
        */

    }
}
