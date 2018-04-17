using System;
using System.Reactive;
using System.Reactive.Linq;
using Acr;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        /// <summary>
        /// Starts connection process if not already connecteds
        /// </summary>
        /// <param name="device"></param>
        /// <param name="config"></param>
        public static void ConnectIf(this IDevice device, GattConnectionConfig config = null)
        {
            if (device.Status == ConnectionStatus.Disconnected)
                device.Connect(config);
        }


        /// <summary>
        /// Waits for connection to actually happen
        /// </summary>
        /// <param name="device"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ConnectWait(this IDevice device, GattConnectionConfig config = null)
            => Observable.Create<IDevice>(ob =>
            {
                var sub = device.WhenConnected().Take(1).Subscribe(_ => ob.Respond(device));
                device.ConnectIf(config);
                return sub;
            });


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

                device.ConnectIf();

                return () =>
                {
                    if (args.DisconnectOnUnsubscribe)
                        device.CancelConnection();

                    sub.Dispose();
                };
            });


        /// <summary>
        /// Attempts to connect to the device, discover the characteristic and write to it
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            var obs = device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(x => x.Write(data))
                .Switch();

            device.ConnectIf();
            return obs;
        }


        /// <summary>
        /// Attempts to connect to device, discover the characteristic and read it
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            var obs = device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();

            device.ConnectIf();
            return obs;
        }


        /// <summary>
        /// Will attempt to connect if necessary, discover the known characteristic, and read on a set interval
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadIntervalCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, TimeSpan timeSpan)
        {
            var obs = device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.ReadInterval(timeSpan))
                .Switch();

            device.ConnectIf();
            return obs;
        }


        /// <summary>
        /// Get known characteristic(s) without service instance
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IDevice device, Guid serviceUuid, params Guid[] characteristicIds) =>
            device
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds))
                .Take(characteristicIds.Length);



        /// <summary>
        /// Discovers all characteristics for a known service
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetCharacteristicsForService(this IDevice device, Guid serviceUuid) =>
            device
                .GetKnownService(serviceUuid)
                .Select(x => x.DiscoverCharacteristics())
                .Switch();

        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<Unit> WhenConnected(this IDevice device) =>
            device
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
