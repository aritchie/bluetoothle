using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    public class CharacteristicTests : AbstractTests
    {
        public CharacteristicTests(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public async Task WriteWithoutResponse()
        {
            var value = new byte[] { 0x01, 0x02 };

            var cs = await this.GetCharacteristics();
            foreach (var ch in cs)
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
            var list = new Dictionary<Guid, int>();
            var characteristics = await CrossBleAdapter
                .Current
                .ScanUntilDeviceFound(Constants.DeviceName)
                .Select(x => x.GetCharacteristicsForKnownService(null))
                .GetCharacteristicsForKnownService(Constants.ScratchServiceUuid)
                .Take(1)
                .Select(x => x.GetKnownService(Constants.ScratchServiceUuid))
                .GetCharacteristics();

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
                        this.Output.WriteLine("Existing characteristic reply - " + id);
                    }
                    else
                    {
                        list.Add(id, 1);
                        this.Output.WriteLine("New characteristic reply - " + id);
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
            var bytes = new byte[] { 0x01 };
            var cs = await this.GetCharacteristics();
            var results = await Observable
                .Merge(
                    cs.ElementAt(0).Write(bytes),
                    cs.ElementAt(1).Write(bytes),
                    cs.ElementAt(2).Write(bytes),
                    cs.ElementAt(3).Write(bytes),
                    cs.ElementAt(4).Write(bytes)
                )
                .Take(5)
                //.Timeout(TimeSpan.FromSeconds(5))
                .ToList();

            results.Count.Should().Be(5);
        }


        [Fact]
        public async Task Concurrent_Reads()
        {
            var cs = await this.GetCharacteristics();
            var results = await Observable
                .Merge(
                    cs.ElementAt(0).Read(),
                    cs.ElementAt(1).Read(),
                    cs.ElementAt(2).Read(),
                    cs.ElementAt(3).Read(),
                    cs.ElementAt(4).Read()
                )
                .Take(5)
                //.Timeout(TimeSpan.FromSeconds(5))
                .ToList();

            results.Count.Should().Be(5);
        }


        [Fact]
        public async Task Cancel_ReleaseLock()
        {
            var bytes = Enumerable.Repeat<byte>(0x01, 20).ToArray();
            var cs = await this.GetCharacteristics();
            try
            {
                await cs.ElementAt(0).Write(bytes).Timeout(TimeSpan.FromSeconds(0));
                throw new ArgumentException("This should not have been hit");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch { }

            await cs.ElementAt(0).Write(bytes).Timeout(TimeSpan.FromSeconds(3));
        }


        [Fact]
        public async Task SequentialWrite()
        {
            var cs = await this.GetCharacteristics();
            await cs.First().Write(new byte[] { 0x01 }).Timeout(TimeSpan.FromSeconds(3));
            await cs.Last().Write(new byte[] { 0x01 }).Timeout(TimeSpan.FromSeconds(3));
        }


        [Fact]
        public async Task SequentialRead()
        {
            var cs = await this.GetCharacteristics();
            var sw = new Stopwatch();

            sw.Start();
            for (var i = 0; i < 5; i++)
            {
                await cs.ElementAt(0).Read();
                await cs.ElementAt(1).Read();
                await cs.ElementAt(2).Read();
                await cs.ElementAt(3).Read();
                await cs.ElementAt(4).Read();
            }
            sw.Stop();
            this.Output.WriteLine($"Reads took {sw.Elapsed.TotalSeconds}s");
        }


        [Fact]
        public async Task NotificationFollowedByWrite()
        {
            var tcs = new TaskCompletionSource<object>();

            var cs = await this.GetCharacteristics();
            var sub = cs
                .First()
                .RegisterAndNotify()
                .Select(x => Observable.FromAsync(async ct =>
                {
                    await x.Characteristic.Write(new byte[] {0x0});
                    await x.Characteristic.Write(new byte[] {0x0});
                    await x.Characteristic.Write(new byte[] {0x0});
                    tcs.TrySetResult(null);
                }))
                .Merge(1)
                .Subscribe();

            await tcs.Task;
            sub.Dispose();
        }
    }
}
