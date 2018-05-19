using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Acr.UserDialogs;
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


        //[Fact]
        //public async Task BlobWriteTest()
        //{
        //    await this.Setup();

        //    this.characteristics[0].BlobWrite()
        //}

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
            var list = new List<Task<CharacteristicGattResult>>();

            foreach (var ch in this.characteristics)
                list.Add(ch.Write(bytes).Timeout(Constants.OperationTimeout).ToTask());

            await Task.WhenAll(list);
        }


        [Fact]
        public async Task Concurrent_Reads()
        {
            await this.Setup();
            var list = new List<Task<CharacteristicGattResult>>();
            foreach (var ch in this.characteristics)
                list.Add(ch.Read().Timeout(Constants.OperationTimeout).ToTask());

            await Task.WhenAll(list);
        }


        [Fact]
        public async Task Reconnect_ReadAndWrite()
        {
            await this.Setup();
            var tcs = new TaskCompletionSource<object>();
            IDisposable floodWriter = null;
            Observable
                .Timer(TimeSpan.FromSeconds(5))
                .Subscribe(async _ =>
                {
                    try
                    {
                        floodWriter?.Dispose();
                        this.device.CancelConnection();

                        await Task.Delay(1000);
                        await this.device.ConnectWait().Timeout(Constants.ConnectTimeout);
                        await this.device
                            .WriteCharacteristic(
                                Constants.ScratchServiceUuid,
                                Constants.ScratchCharacteristicUuid1,
                                new byte[] {0x1}
                            )
                            .Timeout(Constants.OperationTimeout);

                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

            // this is used to flood queue
            floodWriter = this.characteristics
                .ToObservable()
                .Select(x => x.Write(new byte[] { 0x1 }))
                .Merge(4)
                .Repeat()
                //.Switch()
                .Subscribe(
                    x => { },
                    ex => Console.WriteLine(ex)
                );

            await tcs.Task;
        }


        [Fact]
        public async Task NotificationFollowedByWrite()
        {
            await this.Setup();

            var rx = this.characteristics.First();
            var tx = this.characteristics.Last();

            var r = await rx
                .RegisterAndNotify()
                .Take(1)
                .Select(_ => tx.Write(new byte[] {0x0}))
                .Switch()
                .Timeout(Constants.OperationTimeout)
                .FirstOrDefaultAsync();

            Assert.Equal(tx, r.Characteristic);
        }


        [Fact]
        public async Task CancelConnection_RegisterAndNotify()
        {
            await this.Setup();

            var sub = this.characteristics
                .First()
                .RegisterAndNotify()
                .Subscribe();

            this.device.CancelConnection();
            sub.Dispose();

            await Task.Delay(1000);
        }
    }
}
