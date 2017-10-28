using System;
using System.Collections.Generic;
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
    public class Tests : IDisposable
    {
        readonly ITestOutputHelper output;
        IDevice device;


        public Tests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [Fact]
        public async Task AdapterOnOffDetect()
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
        public async Task MultipleCharacteristicSubscriptionsAfterConnect()
        {
            var list = new Dictionary<Guid, int>();
            await this.FindTestDevice();

            await this.device.Connect().Timeout(TimeSpan.FromSeconds(5));
            this.output.WriteLine("Device connected - finding notify characteristics");

            var characteristics = await this.device
                .WhenAnyCharacteristicDiscovered()
                .Where(x => x.CanNotifyOrIndicate())
                .Take(3)
                .ToList()
                .Timeout(TimeSpan.FromSeconds(5))
                .ToTask();

            this.output.WriteLine("Finished characteristic find");
            characteristics.Count.Should().Be(3);


            characteristics
                .ToObservable()
                .Select(x => x.RegisterAndNotify(true))
                .Merge()
                .Synchronize()
                .Subscribe(x =>
                {
                    var id = x.Characteristic.Uuid;
                    if (list.ContainsKey(id))
                    {
                        list[id]++;
                        this.output.WriteLine("Existing characteristic reply - " + id);
                    }
                    else
                    {
                        list.Add(id, 1);
                        this.output.WriteLine("New characteristic reply - " + id);
                    }
                });

            await Task.Delay(10000);

            list.Count.Should().BeGreaterOrEqualTo(2, "There were not at least 2 characteristics in the replies");
            list.First().Value.Should().BeGreaterOrEqualTo(2, "First characteristic did not speak at least 2 times");
            list.ElementAt(2).Value.Should().BeGreaterOrEqualTo(2, "Second characteristic did not speak at least 2 times");
        }


        //[Fact]
        //public async Task GetKwownServiceThenAnother()
        //{
        //    var device = await this.FindTestDevice();
        //    var s1 = await device.GetKnownService(new Guid(""));
        //    var s2 = await device.GetKnownService(new Guid(""));
        //}


        [Fact]
        public async Task Reconnect()
        {
            var connected = 0;
            var disconnected = 0;

            var device = await this.FindTestDevice();
            device
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

            await device.Connect();
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            connected.Should().Be(2, "No reconnection count");
            disconnected.Should().Be(2, "No disconnect");
        }

        //[Fact]
        //public async Task ReconnectAndCharacteristicAction()
        //{

        //}


        async Task<IDevice> FindTestDevice()
        {
            this.output.WriteLine("Finding device");
            this.device = await CrossBleAdapter
                .Current
                .ScanWhenAdapterReady()
                .Select(x => x.Device)
                .Where(x => x.Name?.Equals("Bean+") ?? false)
                .Timeout(TimeSpan.FromSeconds(5))
                .Take(1)
                .ToTask();
            return this.device;
        }


        public void Dispose()
        {
            this.device?.CancelConnection();
        }
    }
}
