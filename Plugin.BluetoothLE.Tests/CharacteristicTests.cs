using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
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
                .Timeout(Constants.DeviceScanTimeout)
                .ToTask();

            await this.device
                .ConnectWait()
                .Timeout(Constants.ConnectTimeout) // android can take some time :P
                .ToTask();

            this.characteristics = await this.device
                .GetCharacteristicsForService(Constants.ScratchServiceUuid)
                .Take(5)
                .Timeout(Constants.OperationTimeout)
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
                await ch.WriteWithoutResponse(value);
                //Assert.True(write.Success, "Write failed - " + write.ErrorMessage);

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

            var sub = this.characteristics
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

            await Task.Delay(Constants.OperationTimeout);
            sub.Dispose();

            Assert.True(list.Count >= 2, "There were not at least 2 characteristics in the replies");
            Assert.True(list.First().Value >= 2, "First characteristic did not speak at least 2 times");
            Assert.True(list.ElementAt(2).Value >= 2, "Second characteristic did not speak at least 2 times");
        }


        [Fact]
        public async Task Concurrent_Writes()
        {
            await this.Setup();
            var bytes = new byte[] { 0x01 };

            var t1 = this.characteristics[0].Write(bytes).Timeout(Constants.OperationTimeout).ToTask();
            var t2 = this.characteristics[1].Write(bytes).Timeout(Constants.OperationTimeout).ToTask();
            var t3 = this.characteristics[2].Write(bytes).Timeout(Constants.OperationTimeout).ToTask();
            var t4 = this.characteristics[3].Write(bytes).Timeout(Constants.OperationTimeout).ToTask();
            var t5 = this.characteristics[4].Write(bytes).Timeout(Constants.OperationTimeout).ToTask();

            await Task.WhenAll(t1, t2, t3, t4, t5);
        }


        [Fact]
        public async Task Concurrent_Reads()
        {
            await this.Setup();
            var t1 = this.characteristics[0].Read().Timeout(Constants.OperationTimeout).ToTask();
            var t2 = this.characteristics[1].Read().Timeout(Constants.OperationTimeout).ToTask();
            var t3 = this.characteristics[2].Read().Timeout(Constants.OperationTimeout).ToTask();
            var t4 = this.characteristics[3].Read().Timeout(Constants.OperationTimeout).ToTask();
            var t5 = this.characteristics[4].Read().Timeout(Constants.OperationTimeout).ToTask();

            await Task.WhenAll(t1, t2, t3, t4, t5);
        }


        [Fact]
        public async Task NotificationFollowedByWrite()
        {
            await this.Setup();

            var r = await this.characteristics
                .First()
                .RegisterAndNotify()
                .Select(x =>
                {
                    return  x.Characteristic.Write(new byte[] {0x0});
                })
                .Switch()
                .Timeout(Constants.OperationTimeout)
                .FirstOrDefaultAsync();

            Assert.True(r.Data != null);
        }
    }
}
