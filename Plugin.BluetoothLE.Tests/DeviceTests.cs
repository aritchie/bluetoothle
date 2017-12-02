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
            this.Device.WhenServiceDiscovered().Subscribe(x =>
            {
                count++;
            });
            await this.Device.Connect(new GattConnectionConfig
            {
                AutoConnect = autoConnect,
                IsPersistent = true
            });
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            await this.Device.WhenStatusChanged().FirstAsync(x => x == ConnectionStatus.Connected);
            //count.Should().Be()
        }


        //[Fact]
        //public async Task Device_GetKnownServices()
        //{
        //    var device = await this.FindTestDevice();
        //    var s1 = await device.GetKnownService(new Guid(""));
        //    var s2 = await device.GetKnownService(new Guid(""));
        //}


        [Fact]
        public async Task Device_Reconnect()
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
/*
 * this.scan = this.BleAdapter
    .ScanWhenAdapterReady()
    //.Where(x => x.AdvertisementData.ServiceUuids.Any(y => y.Equals(ScratchServiceUuid)))
    .Where(x => x.Device?.Name?.StartsWith("bean", StringComparison.CurrentCultureIgnoreCase) ?? false)
    .Take(1)
    .Select(x =>
    {
        this.device = x.Device;
        x.Device.Connect().Subscribe();
        return x.Device.GetKnownService(ScratchServiceUuid);
    })
    .Where(x => x != null)
    .Switch()
    .Select(x => x.WhenCharacteristicDiscovered())
    .Switch()
    .Subscribe(ch =>
    {
        this.WriteMsg("Subscribing to characteristic", ch.Uuid.ToString());
        ch.RegisterAndNotify().Subscribe(x => this.WriteMsg(
            x.Characteristic.Uuid.ToString(),
            UTF8Encoding.UTF8.GetString(x.Data, 0, x.Data.Length)
        ));
    });
 */
