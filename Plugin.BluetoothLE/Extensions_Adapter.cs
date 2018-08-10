using System;
using System.Reactive;
using System.Reactive.Linq;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static bool CanOpenSettings(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.OpenSettings);
        public static bool CanViewPairedDevices(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.ViewPairedDevices);
        public static bool CanControlAdapterState(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.ControlAdapterState);
        public static bool CanPerformLowPoweredScans(this IAdapter adapter) => adapter.Features.HasFlag(AdapterFeatures.LowPoweredScan);


        /// <summary>
        /// Waits for bluetooth adapter to be in an acceptable state and then tries to create a gatt server
        /// </summary>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public static IObservable<IGattServer> WhenReadyCreateServer(this IAdapter adapter) => adapter
            .WhenStatusChanged()
            .Where(x => x == AdapterStatus.PoweredOn)
            .Select(_ => adapter.CreateGattServer())
            .Switch();


        /// <summary>
        /// Fires when adapter is in a powered-on state
        /// </summary>
        /// <param name="adapter"></param>
        /// <returns></returns>
        public static IObservable<IAdapter> WhenReady(this IAdapter adapter) => adapter
            .WhenStatusChanged()
            .Where(x => x == AdapterStatus.PoweredOn)
            .Select(_ => adapter);

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


        /// <summary>
        /// Runs BLE scan for a set timespan then pauses for configured timespan before starting again
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="scanTime"></param>
        /// <param name="scanPauseTime"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IScanResult> ScanInterval(this IAdapter adapter, TimeSpan scanTime, TimeSpan scanPauseTime, ScanConfig config = null) => Observable.Create<IScanResult>(ob =>
        {
            var scanObs = adapter.ScanExtra(config).Do(ob.OnNext, ob.OnError);
            IObservable<long> scanPauseObs = null;
            IObservable<long> scanStopObs = null;

            IDisposable scanSub = null;
            IDisposable scanStopSub = null;
            IDisposable scanPauseSub = null;

            void Scan()
            {
                scanPauseSub?.Dispose();
                scanSub = scanObs.Subscribe();
                scanStopSub = scanStopObs.Subscribe();
            }

            scanPauseObs = Observable.Interval(scanPauseTime).Do(_ => Scan());
            scanStopObs = Observable.Interval(scanTime).Do(_ =>
            {
                scanSub?.Dispose();
                scanStopSub?.Dispose();
                scanPauseSub = scanPauseObs.Subscribe();
            });
            Scan(); // start initial scan

            return () =>
            {
                scanSub?.Dispose();
                scanStopSub?.Dispose();
                scanPauseSub?.Dispose();
            };
        });
    }
}
