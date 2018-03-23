using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;
using Plugin.BluetoothLE.Infrastructure;


namespace Plugin.BluetoothLE.Tests
{
    public abstract class AbstractTests : IDisposable
    {
        protected AbstractTests(ITestOutputHelper output)
        {
            CrossBleAdapter.Current.StopScan();

            this.Output = output;
            Log.Out = (category, msg, lvl) => this.Output.WriteLine($"[{category}] {msg}");
            CrossBleAdapter.Current.Status.Should().Be(AdapterStatus.PoweredOn, "Adapter is not ON");
        }


        public void Dispose() => this.Device?.CancelConnection();


        protected async Task<IDevice> FindTestDevice()
        {
            this.Device = await CrossBleAdapter
                .Current
                .ScanExtra()
                .Select(x => x.Device)
                .Where(x => x.Name?.Equals("Bean+") ?? false)
                .Timeout(TimeSpan.FromSeconds(5))
                .Take(1)
                .ToTask();

            return this.Device;
        }


        protected async Task<IEnumerable<IGattCharacteristic>> GetCharacteristics(TimeSpan? timeout = null)
        {
            var to = timeout ?? TimeSpan.FromSeconds(5);
            this.Device = await this.FindTestDevice();

            this.Device.Connect();
            //this.output.WriteLine("Device connected - finding known service");

            var service = await this.Device
                .GetKnownService(Constants.ScratchServiceUuid)
                .Timeout(to)
                .Take(1)
                .ToTask();
            this.Output.WriteLine("Found known service - detecting characteristics");

            var characteristics = await service
                .DiscoverCharacteristics()
                .Take(5)
                .Timeout(to)
                .ToList()
                .ToTask();

            this.Output.WriteLine("Finished characteristic find");
            characteristics.Count.Should().Be(5);

            return characteristics;
        }


        protected ITestOutputHelper Output { get; }
        protected IDevice Device { get; set; }
    }
}
