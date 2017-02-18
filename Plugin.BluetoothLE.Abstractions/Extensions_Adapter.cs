using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static IObservable<IScanResult> ScanOrListen(this IAdapter adapter)
        {
            return adapter.IsScanning ? adapter.ScanListen() : adapter.Scan();
        }


        public static bool CanOpenSettings(this IAdapter adapter)
        {
            return adapter.Features.HasFlag(AdapterFeatures.OpenSettings);
        }


        public static bool CanViewPairedDevices(this IAdapter adapter)
        {
            return adapter.Features.HasFlag(AdapterFeatures.ViewPairedDevices);
        }


        public static bool CanControlAdapterState(this IAdapter adapter)
        {
            return adapter.Features.HasFlag(AdapterFeatures.ControlAdapterState);
        }


        public static bool CanPerformLowPoweredScans(this IAdapter adapter)
        {
            return adapter.Features.HasFlag(AdapterFeatures.LowPoweredScan);
        }


        public static IObservable<IDevice> ScanForUniqueDevices(this IAdapter adapter)
        {
            return adapter
                .Scan()
                .Distinct(x => x.Device.Uuid)
                .Select(x => x.Device);
        }


        public static IObservable<IScanResult> ScanWhenAdapterReady(this IAdapter adapter)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                IDisposable scan = null;
                var sub = adapter
                    .WhenStatusChanged()
                    .Where(x => x == AdapterStatus.PoweredOn)
                    .Subscribe(x =>
                        scan = adapter.Scan().Subscribe(ob.OnNext)
                    );

                return () =>
                {
                    scan?.Dispose();
                    sub.Dispose();
                };
            });
        }


        public static IObservable<IScanResult> ScanInterval(this IAdapter adapter, TimeSpan timeSpan)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                var scanner = adapter
                    .Scan()
                    .Subscribe(ob.OnNext);

                var timer = Observable
                    .Interval(timeSpan)
                    .Subscribe(x =>
                    {
                        if (scanner == null)
                        {
                            scanner = adapter
                                .Scan()
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
}
