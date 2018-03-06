using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class DeviceTests : AbstractTests
    {
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
        public async Task GetKnownCharacteristics_Consecutively()
        {
            await this.FindTestDevice();
            await this.Device.ConnectWait();
            var s1 = await this.Device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(TimeSpan.FromSeconds(5));

            var s2 = await this.Device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2)
                .Timeout(TimeSpan.FromSeconds(5));

            s1.Should().NotBeNull();
            s2.Should().NotBeNull();
        }


        [Fact]
        public async Task WhenKnownCharacteristic_Fires()
        {
            await this.FindTestDevice();

            this.Device.WhenKnownCharacteristicsDiscovered(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1, Constants.ScratchCharacteristicUuid2);
            await this.Device.ConnectWait();
        }

        [Fact]
        public async Task ReadWriteCharacteristicExtensions()
        {
            var dev = await this.FindTestDevice();
            await Task.WhenAll(
                dev.WriteCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1, new byte[] { 0x01 }).ToTask(),
                dev.ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1).ToTask(),

                dev.WriteCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2, new byte[] { 0x01 }).ToTask(),
                dev.ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2).ToTask(),

                dev.WriteCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid3, new byte[] { 0x01 }).ToTask(),
                dev.ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid3).ToTask(),

                dev.WriteCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid4, new byte[] { 0x01 }).ToTask(),
                dev.ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid4).ToTask(),

                dev.WriteCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid5, new byte[] { 0x01 }).ToTask(),
                dev.ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid5).ToTask()
            );
        }


        [Fact]
        public async Task GetKnownCharacteristics_Concurrent_Notify()
        {
            var c1 = Constants.ScratchCharacteristicUuid1;
            var c2 = Constants.ScratchCharacteristicUuid2;

            await this.FindTestDevice();
            await this.Device.Connect();
            var notifications = await this.Device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, c1, c2)
                .Timeout(TimeSpan.FromSeconds(5))
                .Select(x => x.RegisterAndNotify())
                .Switch()
                .Take(4)
                .ToList();

            notifications.Any(x => x.Characteristic.Uuid.Equals(c1)).Should().BeTrue();
            notifications.Any(x => x.Characteristic.Uuid.Equals(c2)).Should().BeTrue();
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
    }
}
