using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Abstractions;


namespace Plugin.BluetoothLE.Tests
{
    /*
    Service (ad-data) - A495FF20-C5B1-4B44-B512-1370F02D74DE

    // start count
    Scratch 1 - A495FF21-C5B1-4B44-B512-1370F02D74DE

    // temp
    Scratch 2 - A495FF22-C5B1-4B44-B512-1370F02D74DE

    // accel X
    Scratch 3 - A495FF23-C5B1-4B44-B512-1370F02D74DE

    // accel Y
    Scratch 4 - A495FF24-C5B1-4B44-B512-1370F02D74DE

    // accel Z
    Scratch 5 - A495FF25-C5B1-4B44-B512-1370F02D74DE
    */
    public abstract class AbstractTests : IDisposable
    {
        protected AbstractTests(ITestOutputHelper output)
        {
            this.Output = output;
            Log.Out = (category, msg, lvl) => this.Output.WriteLine($"[{category}] {msg}");
            CrossBleAdapter.Current.Status.Should().Be(AdapterStatus.PoweredOn, "Adapter is not ON");
        }


        public void Dispose() => this.Device?.CancelConnection();


        public static Guid ScratchServiceUuid { get; } = new Guid("A495FF20-C5B1-4B44-B512-1370F02D74DE");
        protected async Task<IDevice> FindTestDevice()
        {
            this.Device = await CrossBleAdapter
                .Current
                .ScanWhenAdapterReady()
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

            await this.Device.Connect().Timeout(to);
            //this.output.WriteLine("Device connected - finding known service");

            var service = await this.Device
                .GetKnownService(ScratchServiceUuid)
                .Timeout(to)
                .Take(1)
                .ToTask();
            this.Output.WriteLine("Found known service - detecting characteristics");

            var characteristics = await service
                .WhenCharacteristicDiscovered()
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
