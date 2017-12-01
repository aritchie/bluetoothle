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
        static readonly Guid ScratchServiceUuid = Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE");
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
        readonly ITestOutputHelper output;
        IDevice device;


        public Tests(ITestOutputHelper output)
        {
            this.output = output;
            Log.Out = (category, msg, lvl) => output.WriteLine($"[{category}] {msg}");
            CrossBleAdapter.Current.Status.Should().Be(AdapterStatus.PoweredOn, "Adapter is not ON");
        }


        [Fact]
        public async Task Adapter_Status_Monitor()
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
        public async Task Characteristics_Concurrency_Notifications()
        {
            var list = new Dictionary<Guid, int>();
            var characteristics = await this.GetCharacteristics();

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


        [Fact]
        public async Task Characteristics_Write()
        {
            var cs = await this.GetCharacteristics();
            await cs.First().Write(new byte[] { 0x01 }).Timeout(TimeSpan.FromSeconds(3));
            await cs.Last().Write(new byte[] { 0x01 }).Timeout(TimeSpan.FromSeconds(3));
        }


        [Fact]
        public async Task Characteristics_Concurrency_Writes()
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
        public async Task Characteristics_Concurrency_Reads()
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
        public async Task Characteristics_Cancel_ReleaseLock()
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

        //[Fact]
        //public async Task Device_GetKnownServices()
        //{
        //    var device = await this.FindTestDevice();
        //    var s1 = await device.GetKnownService(new Guid(""));
        //    var s2 = await device.GetKnownService(new Guid(""));
        //}


        [Fact]
        public async Task Device_Reconnect()
        {
            var autoConnect = await UserDialogs.Instance.ConfirmAsync(new ConfirmConfig().SetMessage("Use autoConnect?").UseYesNo());
            var connected = 0;
            var disconnected = 0;

            await this.FindTestDevice();
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

            await this.device.Connect(new GattConnectionConfig
            {
                AutoConnect = autoConnect,
                IsPersistent = true
            });
            await UserDialogs.Instance.AlertAsync("No turn device off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            connected.Should().Be(2, "No reconnection count");
            disconnected.Should().Be(2, "No disconnect");
        }


        async Task<IEnumerable<IGattCharacteristic>> GetCharacteristics()
        {
            await this.FindTestDevice();

            await this.device.Connect().Timeout(TimeSpan.FromSeconds(5));
            this.output.WriteLine("Device connected - finding known service");

            var service = await this.device
                .GetKnownService(ScratchServiceUuid)
                .Timeout(Timeout)
                .Take(1)
                .ToTask();
            this.output.WriteLine("Found known service - detecting characteristics");

            var characteristics = await service
                .WhenCharacteristicDiscovered()
                .Take(5)
                .Timeout(Timeout)
                .ToList()
                .ToTask();

            this.output.WriteLine("Finished characteristic find");
            characteristics.Count.Should().Be(5);

            return characteristics;
        }


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
/*
 * this.scan = this.BleAdapter
    .ScanWhenAdapterReady()
    //.Where(x => x.AdvertisementData.ServiceUuids.Any(y => y.Equals(ScratchServiceUuid)))
    .Where(x => x.Device?.Name?.StartsWith("bean", StringComparison.CurrentCultureIgnoreCase) ?? false)
    .Take(1)
    .Select(x =>
    {
        this.device = x.Device;
        x.Device.Connect().Subscribe();
        return x.Device.GetKnownService(ScratchServiceUuid);
    })
    .Where(x => x != null)
    .Switch()
    .Select(x => x.WhenCharacteristicDiscovered())
    .Switch()
    .Subscribe(ch =>
    {
        this.WriteMsg("Subscribing to characteristic", ch.Uuid.ToString());
        ch.RegisterAndNotify().Subscribe(x => this.WriteMsg(
            x.Characteristic.Uuid.ToString(),
            UTF8Encoding.UTF8.GetString(x.Data, 0, x.Data.Length)
        ));
    });
 */
