using System;
using FluentAssertions;
using Xunit;


namespace Plugin.BluetoothLE.Android.Tests.Tests
{
    public class PlatformTests
    {
        [Theory]
        [InlineData(AdapterFeatures.All, true)]
        [InlineData(AdapterFeatures.AllClient, true)]
        [InlineData(AdapterFeatures.AllControls, true)]
        [InlineData(AdapterFeatures.AllServer, true)]
        [InlineData(AdapterFeatures.None, false)]
        [InlineData(AdapterFeatures.LowPoweredScan, true)]
        [InlineData(AdapterFeatures.OpenSettings, true)]
        [InlineData(AdapterFeatures.ServerGatt, true)]
        [InlineData(AdapterFeatures.ServerAdvertising, true)]
        [InlineData(AdapterFeatures.ViewPairedDevices, true)]
        [InlineData(AdapterFeatures.ControlAdapterState, true)]
        public void FeatureCheck(AdapterFeatures feature, bool expectResult) =>
            CrossBleAdapter.Current.Features.HasFlag(feature).Should().Be(expectResult);


        [Fact]
        public void ControlAdapterStates()
        {
            var stateChanges = 0;
            var ad = CrossBleAdapter.Current;
            ad.WhenStatusChanged().Subscribe(_ => stateChanges++);

            ad.Status.Should().Be(AdapterStatus.PoweredOn);
            ad.SetAdapterState(false);
            ad.Status.Should().Be(AdapterStatus.PoweredOff);
            ad.SetAdapterState(true);
            ad.Status.Should().Be(AdapterStatus.PoweredOn);
            stateChanges.Should().Be(3);
        }
    }
}