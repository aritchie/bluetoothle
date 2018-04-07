using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class CharacteristicTests : IDisposable
    {
        readonly ITestOutputHelper output;
        IGattCharacteristic[] characteristics;
        IDevice device;

        public CharacteristicTests(ITestOutputHelper output) => this.output = output;


        async Task Setup()
        {
            this.device = await CrossBleAdapter
                .Current
                .ScanUntilDeviceFound(Constants.DeviceName)
                .Timeout(TimeSpan.FromSeconds(5000))
                .ToTask();

            await this.device.ConnectWait().ToTask();

            this.characteristics = await this.device
                .GetCharacteristicsForService(Constants.ScratchServiceUuid).Take(5)
                .ToArray()
                .ToTask();
        }


        public void Dispose()
        {
            this.device?.CancelConnection();
        }


        [Fact]
        public async Task WriteWithoutResponse()
        {
            await this.Setup();

            var value = new byte[] { 0x01, 0x02 };
            foreach (var ch in this.characteristics)
            {
                var write = await ch.WriteWithoutResponse(value);
                write.Success.Should().BeTrue("Write failed - " + write.ErrorMessage);

                // TODO: enable write back on host
                //var read = await ch.Read();
                //read.Success.Should().BeTrue("Read failed - " + read.ErrorMessage);

                //read.Data.Should().BeEquivalentTo(value);
            }
        }


        [Fact]
        public async Task Concurrent_Notifications()
        {
            await this.Setup();
            var list = new Dictionary<Guid, int>();

            this.characteristics
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


        [Fact]
        public async Task Concurrent_Writes()
        {
            await this.Setup();
            var bytes = new byte[] { 0x01 };

            var t1 = this.characteristics[0].Write(bytes).ToTask();
            var t2 = this.characteristics[1].Write(bytes).ToTask();
            var t3 = this.characteristics[2].Write(bytes).ToTask();
            var t4 = this.characteristics[3].Write(bytes).ToTask();
            var t5 = this.characteristics[4].Write(bytes).ToTask();

            await Task.WhenAll(t1, t2, t3, t4, t5);

            t1.Result.Success.Should().BeTrue("1 failed");
            t2.Result.Success.Should().BeTrue("2 failed");
            t3.Result.Success.Should().BeTrue("3 failed");
            t4.Result.Success.Should().BeTrue("4 failed");
            t5.Result.Success.Should().BeTrue("5 failed");
        }


        [Fact]
        public async Task Concurrent_Reads()
        {
            await this.Setup();
            var t1 = this.characteristics[0].Read().ToTask();
            var t2 = this.characteristics[1].Read().ToTask();
            var t3 = this.characteristics[2].Read().ToTask();
            var t4 = this.characteristics[3].Read().ToTask();
            var t5 = this.characteristics[4].Read().ToTask();

            await Task.WhenAll(t1, t2, t3, t4, t5);

            t1.Result.Success.Should().BeTrue("1 failed");
            t2.Result.Success.Should().BeTrue("2 failed");
            t3.Result.Success.Should().BeTrue("3 failed");
            t4.Result.Success.Should().BeTrue("4 failed");
            t5.Result.Success.Should().BeTrue("5 failed");
        }


        [Fact]
        public async Task NotificationFollowedByWrite()
        {
            await this.Setup();
            var tcs = new TaskCompletionSource<object>();

            var write = await this.characteristics.First()
                .RegisterAndNotify()
                .Select(x => x.Characteristic.Write(new byte[] {0x0}))
                .Switch()
                .FirstOrDefaultAsync();

            write.Success.Should().BeTrue();
        }
    }
}
