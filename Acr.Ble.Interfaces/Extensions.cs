using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace Acr.Ble
{
    public static class Extensions
    {
        public static IConnectableObservable<TItem> ReplayWithReset<TItem, TReset>(this IObservable<TItem> src, IObservable<TReset> resetTrigger)
        {
            return new ClearableReplaySubject<TItem, TReset>(src, resetTrigger);
        }


        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device)
        {
            return device
                .WhenServiceDiscovered()
                .SelectMany(x => x.WhenCharacteristicDiscovered().Select(y => y));
        }


        public static IObservable<IGattDescriptor> WhenyAnyDescriptorDiscovered(this IDevice device)
        {
            return device
                .WhenAnyCharacteristicDiscovered()
                .SelectMany(x => x.WhenDescriptorDiscovered().Select(y => y));
        }


        public static IObservable<CharacteristicNotification> WhenAnyCharacteristicNotificationReceived(this IDevice device, bool doSubscriptions)
        {
            return Observable.Create<CharacteristicNotification>(ob =>
            {
                var list = new List<IDisposable>();

                var all = device
                    .WhenAnyCharacteristicDiscovered()
                    .Subscribe(ch =>
                    {
                        if (doSubscriptions)
                        {
                            list.Add(ch
                                .SubscribeToNotifications()
                                .Subscribe(data => Trigger(ob, ch, data))
                            );
                        }
                        else
                        {
                            list.Add(ch
                                .WhenNotificationReceived()
                                .Subscribe(data => Trigger(ob, ch, data))
                            );
                        }
                    });

                list.Add(all);

                return () =>
                {
                    foreach (var item in list)
                        item.Dispose();
                };
            });
        }


        public static async Task<IList<IGattCharacteristic>> GetAllCharacteristics(this IDevice device, int waitMillis = 2000)
        {
            var result = await device
                .WhenAnyCharacteristicDiscovered()
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(waitMillis))
                .ToList();

            return result;
        }


        public static async Task<IList<IGattDescriptor>> GetAllDescriptors(this IDevice device, int waitMillis = 2000)
        {
            var result = await device
                .WhenyAnyDescriptorDiscovered()
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(waitMillis))
                .ToList();

            return result;
        }


        public static IObservable<byte[]> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
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


        public static IObservable<byte[]> WhenReadOrNotify(this IGattCharacteristic character, TimeSpan readInterval)
        {
            if (character.CanNotify())
                return character.SubscribeToNotifications();

            if (character.CanRead())
                return character.ReadInterval(readInterval);

            throw new ArgumentException($"Characteristic {character.Uuid} does not have read or notify permissions");
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


        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }


        public static bool CanWriteWithResponse(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.Write);
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


        public static void AssertWrite(this IGattCharacteristic characteristic, bool withResponse)
        {
            if (!characteristic.CanWrite())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support writes");

            if (withResponse && !characteristic.CanWriteWithResponse())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support writes with response");
        }


        public static void AssertRead(this IGattCharacteristic characteristic)
        {
            if (!characteristic.CanRead())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support reads");
        }


        public static void AssertNotify(this IGattCharacteristic characteristic)
        {
            if (!characteristic.CanNotify())
                throw new ArgumentException($"This characteristic '{characteristic.Uuid}' does not support notifications");
        }


        static void Trigger(IObserver<CharacteristicNotification> ob, IGattCharacteristic characteristic, byte[] data)
        {
            var notification = new CharacteristicNotification(characteristic, data);
            ob.OnNext(notification);
        }
    }
}
