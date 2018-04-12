using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static CharacteristicGattResult ToResult(this IGattCharacteristic ch, GattEvent gattEvent, string message)
            => new CharacteristicGattResult(ch, gattEvent, message);


        public static CharacteristicGattResult ToResult(this IGattCharacteristic ch, GattEvent gattEvent, byte[] data)
            => new CharacteristicGattResult(ch, gattEvent, data);


        public static DescriptorGattResult ToResult(this IGattDescriptor desc, GattEvent gattEvent, string message)
            => new DescriptorGattResult(desc, gattEvent, message);


        public static DescriptorGattResult ToResult(this IGattDescriptor desc, GattEvent gattEvent, byte[] data)
            => new DescriptorGattResult(desc, gattEvent, data);


        /// <summary>
        /// Enables notifications and hooks it for discovered characteristic.  When subscription is disposed, it will also clean up.
        /// </summary>
        /// <param name="characteristic"></param>
        /// <param name="useIndicationIfAvailable"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> RegisterAndNotify(this IGattCharacteristic characteristic, bool useIndicationIfAvailable = false)
            => characteristic
                .EnableNotifications(useIndicationIfAvailable)
                .Select(x =>
                {
                    if (x.Success)
                        return characteristic.WhenNotificationReceived();

                    return Observable.Return(x);
                })
                .Switch()
                .Finally(() => characteristic
                    .DisableNotifications()
                    .Subscribe()
                );


        //public static IObservable<CharacteristicGattResult> ReadUntil(this IGattCharacteristic characteristic, byte[] endBytes)
        //    => Observable.Create<CharacteristicGattResult>(async ob =>
        //    {
        //        var cancelSrc = new CancellationTokenSource();
        //        try
        //        {
        //            var result = await characteristic.Read().RunAsync(cancelSrc.Token);
        //            while (!result.Data.SequenceEqual(endBytes) && !cancelSrc.IsCancellationRequested)
        //            {
        //                ob.OnNext(result);
        //                result = await characteristic.Read().RunAsync(cancelSrc.Token);
        //            }
        //            ob.OnCompleted();
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            // swallow
        //        }
        //        return () => cancelSrc.Cancel();
        //    });


        public static IObservable<CharacteristicGattResult> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
            => Observable
                .Interval(timeSpan)
                .Select(_ => character.Read())
                .Switch();


        public static bool CanRead(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Read);
        public static bool CanWriteWithResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.Write);
        public static bool CanWriteWithoutResponse(this IGattCharacteristic ch) => ch.Properties.HasFlag(CharacteristicProperties.WriteNoResponse);
        public static bool CanWrite(this IGattCharacteristic ch)
            => ch.Properties.HasFlag(CharacteristicProperties.WriteNoResponse) || ch.Properties.HasFlag(CharacteristicProperties.Write);


        public static bool CanNotifyOrIndicate(this IGattCharacteristic ch) => ch.CanNotify() || ch.CanIndicate();


        public static bool CanNotify(this IGattCharacteristic ch) =>
            ch.Properties.HasFlag(CharacteristicProperties.Notify) ||
            ch.Properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired) ||
            ch.CanIndicate();


        public static bool CanIndicate(this IGattCharacteristic ch) =>
            ch.Properties.HasFlag(CharacteristicProperties.Indicate) ||
            ch.Properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired);


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
