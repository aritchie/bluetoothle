using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;


namespace Acr.Ble
{
    public static class Extensions
    {
        public static IObservable<IScanResult> ScanOrListen(this IAdapter adapter)
        {
            return adapter.IsScanning ? adapter.ScanListen() : adapter.Scan();
        }


        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device)
        {
            return device.WhenServiceDiscovered().SelectMany(x => x.WhenCharacteristicDiscovered());
        }


        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IDevice device)
        {
            return device.WhenAnyCharacteristicDiscovered().SelectMany(x => x.WhenDescriptorDiscovered());
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


        public static bool IsPairingAvailable(this IDevice device)
        {
            return device.Features.HasFlag(DeviceFeatures.PairingRequests);
        }


        public static bool IsMtuRequestAvailable(this IDevice device)
        {
            return device.Features.HasFlag(DeviceFeatures.MtuRequests);
        }


        public static bool IsReliableTransactionsAvailable(this IDevice device)
        {
            return device.Features.HasFlag(DeviceFeatures.ReliableTransactions);
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


        public static IObservable<CharacteristicResult> ReadUntil(this IGattCharacteristic characteristic, byte[] endBytes)
        {
            return Observable.Create<CharacteristicResult>(async ob =>
            {
                var cancelSrc = new CancellationTokenSource();
                try
                {
                    var result = await characteristic.Read().RunAsync(cancelSrc.Token);
                    while (!result.Data.SequenceEqual(endBytes))
                    {
                        ob.OnNext(result);
                        result = await characteristic.Read().RunAsync(cancelSrc.Token);
                    }
                    ob.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    // swallow
                }
                return () => cancelSrc.Cancel();
            });
        }


        public static IConnectableObservable<TItem> ReplayWithReset<TItem, TReset>(this IObservable<TItem> src, IObservable<TReset> resetTrigger)
        {
            return new ClearableReplaySubject<TItem, TReset>(src, resetTrigger);
        }


        public static IObservable<CharacteristicResult> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
        {
            return Observable.Create<CharacteristicResult>(ob =>
                Observable
                    .Interval(timeSpan)
                    .Subscribe(async _ =>
                    {
                        var read = await character.Read();
                        ob.OnNext(read);
                    })
            );
        }


        public static IObservable<CharacteristicResult> WhenReadOrNotify(this IGattCharacteristic character, TimeSpan readInterval)
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



        public static bool CanWriteWithoutResponse(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.WriteNoResponse);
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
            return ch.Properties.HasFlag(CharacteristicProperties.Notify) ||
                   ch.Properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired) ||
                   ch.Properties.HasFlag(CharacteristicProperties.Indicate) ||
                   ch.Properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired);
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
    }
}
