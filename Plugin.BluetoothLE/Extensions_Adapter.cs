using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static bool CanOpenSettings(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.OpenSettings);
        public static bool CanViewPairedDevices(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.ViewPairedDevices);
        public static bool CanControlAdapterState(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.ControlAdapterState);
        public static bool CanPerformLowPoweredScans(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.LowPoweredScan);


        /// <summary>
        /// This will scan until the device a specific device is found, then cancel the scan
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="deviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ScanUntilDeviceFound(this IAdapter adapter, Guid deviceUuid) => adapter
            .Scan()
            .Where(x => x.Device.Uuid.Equals(deviceUuid))
            .Take(1)
            .Select(x => x.Device);


        /// <summary>
        /// This will scan until the device a specific device is found, then cancel the scan
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="deviceName"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ScanUntilDeviceFound(this IAdapter adapter, string deviceName) => adapter
            .Scan()
            .Where(x => x.Device.Name?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) ?? false)
            .Take(1)
            .Select(x => x.Device);


        //public static IObservable<IScanResult> ScanTimed(this IAdapter adapter, TimeSpan scanTime, ScanConfig config = null) => adapter
        //    .Scan(config)
        //    .Take(scanTime);

        /// <summary>
        /// Scans only for distinct devices instead of repeating each device scan response - this will only give you devices, not RSSI or ad packets
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ScanForUniqueDevices(this IAdapter adapter, ScanConfig config = null) => adapter
            .Scan(config)
            .Distinct(x => x.Device.Uuid)
            .Select(x => x.Device);


        /// <summary>
        /// This method wraps the traditional scan, but waits for the adapter to be ready before initiating scan
        /// </summary>
        /// <param name="adapter">The adapter to scan with</param>
        /// <param name="restart">Stops any current scan running</param>
        /// <param name="config">ScanConfig parameters you would like to use</param>
        /// <returns></returns>
        public static IObservable<IScanResult> ScanExtra(this IAdapter adapter, ScanConfig config = null, bool restart = false) => adapter
            .WhenStatusChanged()
            .Where(x => x == AdapterStatus.PoweredOn)
            .Select(_ =>
            {
                if (restart && adapter.IsScanning)
                {
                    adapter.StopScan(); // need a pause to wait for scan to end
                }
                return adapter.Scan(config);
            })
            .Switch();
    }
}
