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
    public class SpecificTests
    {
        readonly ITestOutputHelper output;


        public SpecificTests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [Fact]
        public async Task MultipleCharacteristicSubscriptionsAfterConnect()
        {
            var list = new Dictionary<Guid, int>();

            var device = await this.FindTestDevice();
            device
                .WhenAnyCharacteristicDiscovered()
                .Where(x => x.CanNotifyOrIndicate())
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
            
            await device.Connect().Timeout(TimeSpan.FromSeconds(5));
            await Task.Delay(10000);

            list.Count.Should().BeGreaterOrEqualTo(2, "There were not at least 2 characteristics in the replies");
            list.First().Value.Should().BeGreaterOrEqualTo(2, "First characteristic did not speak at least 2 times");
            list.ElementAt(2).Value.Should().BeGreaterOrEqualTo(2, "Second characteristic did not speak at least 2 times");
        }


        [Fact]
        public async Task GetKwownServiceThenAnother()
        {
            var device = await this.FindTestDevice();
            var s1 = await device.GetKnownService(new Guid(""));
            var s2 = await device.GetKnownService(new Guid(""));
        }


        Task<IDevice> FindTestDevice() => CrossBleAdapter
            .Current
            .ScanWhenAdapterReady()
            .Select(x => x.Device)
            .Where(x => x.Name?.Equals("Bean+") ?? false)
            .Timeout(TimeSpan.FromSeconds(5))
            .Take(1)
            .ToTask();
    }
}
