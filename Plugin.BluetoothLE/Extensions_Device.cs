using System;
using System.Reactive;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static IObservable<Unit> ConnectWait(this IDevice device)
        {
            device.Connect();
            return device.WhenConnected();
        }


        public static IObservable<CharacteristicGattResult> ConnectHook(this IDevice device, Guid serviceUuid, params Guid[] characteristicUuuids)
            => device.ConnectHook(new ConnectHookArgs(serviceUuid, characteristicUuuids));


        /// <summary>
        /// This method will connect and manage connection as well as hook into your required characteristics with all the proper cleanups necessary
        /// </summary>
        /// <param name="device"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ConnectHook(this IDevice device, ConnectHookArgs args)
            => Observable.Create<CharacteristicGattResult>(ob =>
            {
                var sub = device
                    .WhenConnected()
                    .Select(_ => device.WhenKnownCharacteristicsDiscovered(args.ServiceUuid, args.CharacteristicUuids))
                    .Switch()
                    .Select(x => x.RegisterAndNotify(args.UseIndicateIfAvailable))
                    .Switch()
                    .Subscribe(ob.OnNext);

                if (device.Status == ConnectionStatus.Disconnected)
                    device.Connect();

                return () =>
                {
                    if (args.DisconnectOnUnsubscribe)
                        device.CancelConnection();

                    sub.Dispose();
                };
            });


        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            device.Connect();

            return device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(x => x.Write(data))
                .Switch();
        }


        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            device.Connect();

            return device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();
        }


        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IDevice device, Guid serviceUuid, params Guid[] characteristicIds)
            => device
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds))
                .Take(characteristicIds.Length);


        public static IObservable<Unit> WhenConnected(this IDevice device)
            => device
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Connected)
                .Select(x => Unit.Default);


        public static IObservable<IGattCharacteristic> WhenKnownCharacteristicsDiscovered(this IDevice device,
            Guid serviceUuid, params Guid[] characteristicIds)
            => device
                .WhenConnected()
                .Select(_ => device.GetKnownCharacteristics(serviceUuid, characteristicIds))
                .Switch();


        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device)
            => device.WhenServiceDiscovered().SelectMany(x => x.WhenCharacteristicDiscovered());


        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IDevice device)
            => device.WhenAnyCharacteristicDiscovered().SelectMany(x => x.WhenDescriptorDiscovered());


        public static bool IsPairingAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.PairingRequests);


        public static bool IsMtuRequestAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.MtuRequests);


        public static bool IsReliableTransactionsAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.ReliableTransactions);
    }
}
