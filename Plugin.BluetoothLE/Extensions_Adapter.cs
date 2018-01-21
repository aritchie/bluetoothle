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
        public static IObservable<IScanResult> ScanExtra(this IAdapter adapter, ScanConfig config = null, bool restart = false) =>adapter
            .WhenStatusChanged()
            .Where(x => x == AdapterStatus.PoweredOn)
            .Select(_ =>
            {
                if (restart && adapter.IsScanning)
                    adapter.StopScan();

                return adapter.Scan(config);
            })
            .Switch();


        public static IObservable<IScanResult> ScanInterval(this IAdapter adapter, TimeSpan timeSpan, ScanConfig config = null)
            => Observable.Create<IScanResult>(ob =>
            {
                var scanner = adapter
                    .Scan(config)
                    .Subscribe(ob.OnNext);

                var timer = Observable
                    .Interval(timeSpan)
                    .Subscribe(x =>
                    {
                        if (scanner == null)
                        {
                            scanner = adapter
                                .Scan(config)
                                .Subscribe(ob.OnNext);
                        }
                        else
                        {
                            scanner.Dispose();
                            scanner = null;
                        }
                    });

                return () =>
                {
                    timer.Dispose();
                    scanner?.Dispose();
                };
            });
    }
}
