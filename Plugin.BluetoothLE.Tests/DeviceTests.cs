using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Xunit;


namespace Plugin.BluetoothLE.Tests
{
    public class DeviceTests : IDisposable
    {
        IDevice device;


        public void Dispose()
        {
            this.device?.CancelConnection();
        }


        async Task Setup(bool connect)
        {
            this.device = await CrossBleAdapter
                .Current
                .ScanUntilDeviceFound(Constants.DeviceName)
                .ToTask();

            if (connect)
                await this.device.ConnectWait().ToTask();
        }


        [Fact]
        public async Task GetConnectedDevices()
        {
            await this.Setup(true);
            var devices = CrossBleAdapter.Current.GetConnectedDevices();
            Assert.Equal(1, devices.Count());

            Assert.True(devices.First().Uuid.Equals(this.device.Uuid));
            this.device.CancelConnection();
            await Task.Delay(2000); // wait for dc to occur

            Assert.Equal(ConnectionStatus.Disconnected, this.device.Status);
            devices = CrossBleAdapter.Current.GetConnectedDevices();
            Assert.Equal(0, devices.Count());
        }


        [Fact]
        public async Task WhenServiceFound_Reconnect_ShouldFlushOriginals()
        {
            await this.Setup(false);

            var count = 0;
            this.device.DiscoverServices().Subscribe(_ => count++);

            await this.device.ConnectWait().ToTask();
            await UserDialogs.Instance.AlertAsync("Now turn device off & press OK");
            var origCount = count;
            count = 0;

            await UserDialogs.Instance.AlertAsync("No turn device back on & press OK when light turns green");
            await Task.Delay(5000);
            Assert.Equal(count, origCount);
        }


        [Fact]
        public async Task GetKnownCharacteristics_Consecutively()
        {
            await this.Setup(true);

            var s1 = await this.device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(TimeSpan.FromSeconds(5))
                .ToTask();

            var s2 = await this.device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2)
                .Timeout(TimeSpan.FromSeconds(5))
                .ToTask();

            Assert.NotNull(s1);
            Assert.NotNull(s2);
        }


        [Fact]
        public async Task WhenKnownCharacteristic_Fires()
        {
            await this.Setup(true);

            await this.device
                .ConnectWait()
                .Select(x => x.WhenKnownCharacteristicsDiscovered(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    Constants.ScratchCharacteristicUuid2
                ))
                .Take(5)
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask()
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadWriteCharacteristicExtensions()
        {
            await this.Setup(false);

            await this.device
                .WriteCharacteristic(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    new byte[] {0x01}
                )
                .Timeout(TimeSpan.FromSeconds(7))
                .ToTask();

            await this.device
                .ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(TimeSpan.FromSeconds(7))
                .ToTask();
        }


        [Fact]
        public async Task Extensions_HookCharacteristic()
        {
            await this.Setup(false);
            //this.device.ConnectHook(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
            //    .Subscribe(XmlAssertionExtensions =>
            //    {

            //    })
        }


        [Fact]
        public async Task ReconnectTest()
        {
            var autoConnect = await UserDialogs.Instance.ConfirmAsync(new ConfirmConfig().SetMessage("Use autoConnect?").UseYesNo());
            var connected = 0;
            var disconnected = 0;

            await this.Setup(false);
            this.device
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

            await this.device.ConnectWait(new GattConnectionConfig
            {
                AndroidAutoConnect = autoConnect,
                IsPersistent = true
            });
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            Assert.Equal(2, connected);
            Assert.Equal(2, disconnected);
        }
    }
}
