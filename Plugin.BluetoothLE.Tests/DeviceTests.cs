using System;
using System.Collections.Generic;
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
                .Timeout(Constants.DeviceScanTimeout)
                .ToTask();

            if (connect)
                await this.device
                    .ConnectWait()
                    .Timeout(Constants.ConnectTimeout)
                    .ToTask();
        }


        [Fact]
        public async Task Service_Rediscover()
        {
            await this.Setup(true);
            var services1 = await this.device
                .GetCharacteristicsForService(Constants.ScratchServiceUuid)
                .Timeout(Constants.OperationTimeout)
                .ToList()
                .ToTask();

            var services2 = await this.device
                .GetCharacteristicsForService(Constants.ScratchServiceUuid)
                .Timeout(Constants.OperationTimeout)
                .ToList()
                .ToTask();

            Assert.Equal(services1.Count, services2.Count);
        }


        //[Fact]
        //public async Task GetConnectedDevices()
        //{
        //    await this.Setup(true);
        //    var devices = await CrossBleAdapter.Current.GetConnectedDevices().ToTask();
        //    Assert.Equal(1, devices.Count());

        //    Assert.True(devices.First().Uuid.Equals(this.device.Uuid));
        //    this.device.CancelConnection();
        //    await Task.Delay(2000); // wait for dc to occur

        //    Assert.Equal(ConnectionStatus.Disconnected, this.device.Status);
        //    devices = await CrossBleAdapter.Current.GetConnectedDevices().ToTask();
        //    Assert.Equal(0, devices.Count());
        //}


        [Fact]
        public async Task KnownCharacteristics_GetKnownCharacteristics_Consecutively()
        {
            await this.Setup(true);

            var s1 = await this.device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            var s2 = await this.device
                .GetKnownCharacteristics(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2)
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            Assert.NotNull(s1);
            Assert.NotNull(s2);
        }


        [Fact]
        public async Task KnownCharacteristics_WhenKnownCharacteristics()
        {
            await this.Setup(true);

            var tcs = new TaskCompletionSource<object>();
            var results = new List<IGattCharacteristic>();
            this.device
                .WhenKnownCharacteristicsDiscovered(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    Constants.ScratchCharacteristicUuid2
                )
                .Subscribe(
                    results.Add,
                    ex => tcs.SetException(ex),
                    () => tcs.SetResult(null)
                );

            await this.device.ConnectWait();
            await Task.WhenAny(
                tcs.Task,
                Task.Delay(5000)
            );

            Assert.Equal(2, results.Count);
            Assert.True(results.Any(x => x.Uuid.Equals(Constants.ScratchCharacteristicUuid1)));
            Assert.True(results.Any(x => x.Uuid.Equals(Constants.ScratchCharacteristicUuid2)));
        }


        [Fact]
        public async Task Extension_ReadWriteCharacteristic()
        {
            await this.Setup(false);

            await this.device
                .WriteCharacteristic(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    new byte[] { 0x01 }
                )
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            await this.device
                .ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(Constants.OperationTimeout)
                .ToTask();
        }


        [Fact]
        public async Task Extension_HookCharacteristic()
        {
            await this.Setup(false);
            var list = await this.device
                .ConnectHook(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1
                )
                .Take(3)
                .ToList()
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            Assert.Equal(3, list.Count);
        }


        [Fact]
        public async Task ConnectHook_Reconnect()
        {
            await this.Setup(false);
            var count = 0;

            var sub = this.device
                .ConnectHook(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Subscribe(x => count++);

            await this.device.WhenConnected().Take(1).ToTask();
            var disp = UserDialogs.Instance.Alert("Now turn off device and wait");
            await this.device.WhenDisconnected().Take(1).ToTask();
            count = 0;
            disp.Dispose();

            await Task.Delay(1000);
            disp = UserDialogs.Instance.Alert("Now turn device on and wait");
            await this.device.WhenConnected().Take(1).ToTask();
            disp.Dispose();

            await Task.Delay(3000);
            sub.Dispose();
            Assert.True(count > 0, "No pings");
        }


        [Fact]
        public async Task ReconnectTest()
        {
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

            await this.device.ConnectWait();
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            Assert.Equal(2, connected);
            Assert.Equal(2, disconnected);
        }


        [Fact]
        public async Task Reconnect_WhenServiceFound_ShouldFlushOriginals()
        {
            await this.Setup(false);

            var count = 0;
            this.device.DiscoverServices().Subscribe(_ => count++);

            await this.device.ConnectWait().ToTask();
            await UserDialogs.Instance.AlertAsync("Now turn device off & press OK");
            var origCount = count;
            count = 0;

            await UserDialogs.Instance.AlertAsync("Now turn device back on & press OK when light turns green");
            await Task.Delay(5000);
            Assert.Equal(count, origCount);
        }
    }
}
