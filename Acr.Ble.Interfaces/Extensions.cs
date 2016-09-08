using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Acr.Ble
{
    public static class Rx
    {
        public static async Task<IList<IGattCharacteristic>> GetAllCharacteristics(this IDevice device, int waitMillis = 2000)
        {
            var characteristics = await device
                .WhenServiceDiscovered()
                .SelectMany(x => x.WhenCharacteristicDiscovered().Select(y => y))
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(waitMillis))
                .ToList();
            return characteristics;
        }


        public static IObservable<byte[]> PeriodicRead(this IGattCharacteristic character, TimeSpan timeSpan)
        {
            return Observable.Create<byte[]>(ob =>
                Observable
                    .Interval(timeSpan)
                    .Subscribe(async _ =>
                    {
                        var read = await character.Read();
                        ob.OnNext(read);
                    })
            );
        }


        public static IObservable<IScanResult> PeriodicScan(this IAdapter adapter, TimeSpan timeSpan, ScanFilter filter = null)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                var scanner = adapter
                    .Scan(filter)
                    .Subscribe(ob.OnNext);

                var timer = Observable
                    .Interval(timeSpan)
                    .Subscribe(x =>
                    {
                        if (scanner == null)
                        {
                            scanner = adapter
                                .Scan(filter)
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


        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }


        public static bool CanWrite(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.WriteNoResponse) ||
                    ch.Properties.HasFlag(CharacteristicProperties.Write);
        }


        public static bool CanRead(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.Read);
        }


        public static bool CanNotify(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.Notify);
        }
    }
}
