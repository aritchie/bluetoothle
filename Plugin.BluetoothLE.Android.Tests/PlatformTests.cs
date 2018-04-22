using System;
using System.Collections.Generic;
using Xunit;


namespace Plugin.BluetoothLE.Android.Tests
{
    public class PlatformTests
    {
        [Theory]
        [MemberData(nameof(GetUuids))]
        public void UuidToGuidTests(string uuidString, byte[] bytes)
            => Assert.Equal(uuidString, bytes.ToGuid().ToString().ToUpper());


        public static IEnumerable<object[]> GetUuids()
        {
            yield return new object[]
            {
                "63331358-23C1-11E5-B696-FEFF819CDC9F",
                new byte[] { 0x9f, 0xdc, 0x9c, 0x81, 0xff, 0xfe, 0x96, 0xb6, 0xe5, 0x11, 0xc1, 0x23, 0x58, 0x13, 0x33, 0x63 }
            };
        }


        [Theory]
        [InlineData(AdapterFeatures.None, false)]
        [InlineData(AdapterFeatures.All, true)]
        [InlineData(AdapterFeatures.AllClient, true)]
        [InlineData(AdapterFeatures.AllControls, true)]
        [InlineData(AdapterFeatures.AllServer, true)]
        [InlineData(AdapterFeatures.LowPoweredScan, true)]
        [InlineData(AdapterFeatures.OpenSettings, true)]
        [InlineData(AdapterFeatures.ServerGatt, true)]
        [InlineData(AdapterFeatures.ServerAdvertising, true)]
        [InlineData(AdapterFeatures.ViewPairedDevices, true)]
        [InlineData(AdapterFeatures.ControlAdapterState, true)]
        public void FeatureCheck(AdapterFeatures feature, bool expectResult) =>
            Assert.Equal(CrossBleAdapter.Current.Features.HasFlag(feature), expectResult);


        [Fact]
        public void ControlAdapterStates()
        {
            var stateChanges = 0;
            var ad = CrossBleAdapter.Current;
            ad.WhenStatusChanged().Subscribe(_ => stateChanges++);

            Assert.Equal(AdapterStatus.PoweredOn, ad.Status);
            ad.SetAdapterState(false);
            Assert.Equal(AdapterStatus.PoweredOff, ad.Status);
            ad.SetAdapterState(true);
            Assert.Equal(AdapterStatus.PoweredOn, ad.Status);
            Assert.Equal(3, stateChanges);
        }
    }
}