using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device)
        {
            return device.WhenServiceDiscovered().SelectMany(x => x.WhenCharacteristicDiscovered());
        }


        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IDevice device)
        {
            return device.WhenAnyCharacteristicDiscovered().SelectMany(x => x.WhenDescriptorDiscovered());
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
    }
}
