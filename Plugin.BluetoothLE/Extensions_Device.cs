using System;
using System.Reactive;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        /// <summary>
        /// Waits for connection to actually happen
        /// </summary>
        /// <param name="device"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ConnectWait(this IDevice device, GattConnectionConfig config = null)
        {
            device.Connect(config);
            return device
                .WhenConnected()
                .Select(_ => device);
        }


        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuuids"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ConnectHook(this IDevice device, Guid serviceUuid, params Guid[] characteristicUuuids)
            => device.ConnectHook(new ConnectHookArgs(serviceUuid, characteristicUuuids));


        /// <summary>
        /// Connect and manage connection as well as hook into your required characteristics with all the proper cleanups necessary
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


        /// <summary>
        /// Writes to a characteristic without need for instance
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            device.Connect();

            return device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(x => x.Write(data))
                .Switch();
        }


        /// <summary>
        /// /// Reads a characteristic without need for instance
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            device.Connect();

            return device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();
        }


        /// <summary>
        /// Get known characteristic(s) without service instance
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IDevice device, Guid serviceUuid, params Guid[] characteristicIds)
            => device
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds))
                .Take(characteristicIds.Length);


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<Unit> WhenConnected(this IDevice device)
            => device
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Connected)
                .Select(x => Unit.Default);


        /// <summary>
        /// Will call GetKnownCharacteristics when connected state occurs
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenKnownCharacteristicsDiscovered(this IDevice device, Guid serviceUuid, params Guid[] characteristicIds) =>
            device
                .WhenConnected()
                .Select(_ => device.GetKnownCharacteristics(serviceUuid, characteristicIds))
                .Switch();


        /// <summary>
        /// Will discover all services/characteristics when connected state occurs
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IDevice device) =>
            device
                .WhenConnected()
                .Select(x => device.DiscoverServices())
                .Switch()
                .SelectMany(x => x.DiscoverCharacteristics());


        /// <summary>
        /// Will discover all services/characteristics/descriptors when connected state occurs
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IDevice device)
            => device.WhenAnyCharacteristicDiscovered().SelectMany(x => x.DiscoverDescriptors());


        public static bool IsPairingAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.PairingRequests);


        public static bool IsMtuRequestAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.MtuRequests);


        public static bool IsReliableTransactionsAvailable(this IDevice device)
            => device.Features.HasFlag(DeviceFeatures.ReliableTransactions);
    }
}
