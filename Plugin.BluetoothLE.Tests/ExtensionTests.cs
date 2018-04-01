using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using Xunit;


namespace Plugin.BluetoothLE.Tests
{
    public class ExtensionTests
    {

        [Fact]
        public async Task FindAndWrite()
        {
            var result = await CrossBleAdapter
                .Current
                .ScanUntilDeviceFound(Constants.DeviceName)
                .Select(x => x.WriteCharacteristic(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    new byte[] { 0x01 }
                ))
                .Switch()
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask()
                .ConfigureAwait(false);

            result.Success.Should().Be(true);
            //result.Data.Should().Be()
        }


        [Fact]
        public async Task FindAndRead()
        {
            var result = await CrossBleAdapter
                .Current
                .ScanUntilDeviceFound(Constants.DeviceName)
                .Select(x => x.ReadCharacteristic(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1
                ))
                .Switch()
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask()
                .ConfigureAwait(false);

            result.Success.Should().Be(true);
            //result.Data.Should().Be()
        }
    }
}
