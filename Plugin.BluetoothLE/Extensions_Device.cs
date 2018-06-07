using System;
using System.Reactive.Linq;
using Acr.Reactive;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {
        public static bool IsConnected(this IDevice device) => device.Status == ConnectionStatus.Connected;
        public static bool IsDisconnected(this IDevice device) => !device.IsConnected();


        /// <summary>
        /// Starts connection process if not already connecteds
        /// </summary>
        /// <param name="device"></param>
        public static void ConnectIf(this IDevice device)
        {
            if (device.Status == ConnectionStatus.Disconnected)
                device.Connect();
        }


        /// <summary>
        /// Continuously reads RSSI from a connected device
        /// WARNING: you really don't want to run this with an Android GATT connection
        /// </summary>
        /// <param name="device"></param>
        /// <param name="readInterval"></param>
        /// <returns></returns>
        public static IObservable<int> ReadRssiContinuously(this IDevice device, TimeSpan? readInterval = null) => Observable
            .Interval(readInterval ?? TimeSpan.FromSeconds(1))
            .Select(_ => device.ReadRssi())
            .Switch();


        /// <summary>
        /// When device is connected, this will call for RSSI continuously
        /// </summary>
        /// <param name="device"></param>
        /// <param name="readInterval"></param>
        /// <returns></returns>
        public static IObservable<int> WhenReadRssiContinuously(this IDevice device, TimeSpan? readInterval = null)
            => device
                .WhenConnected()
                .Select(x => x.ReadRssiContinuously(readInterval))
                .Switch();


        /// <summary>
        /// Waits for connection to actually happen
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<IDevice> ConnectWait(this IDevice device)
            => Observable.Create<IDevice>(ob =>
            {
                var sub1 = device
                    .WhenConnected()
                    .Take(1)
                    .Subscribe(_ => ob.Respond(device));

                var sub2 = device
                    .WhenConnectionFailed()
                    .Subscribe(ob.OnError);

                device.ConnectIf();
                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                    if (device.Status != ConnectionStatus.Connected)
                        device.CancelConnection();
                };
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
                    .Select(x => x.RegisterAndNotify(args.UseIndicateIfAvailable, false))
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
        /// Discover the characteristic and write to it
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
            => device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(x =>
                {
                    return x.Write(data);
                })
                .Switch();


        /// <summary>
        /// Discover the characteristic and read it
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid)
            => device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();


        /// <summary>
        /// Discover the known characteristic, and read on a set interval
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadIntervalCharacteristic(this IDevice device, Guid serviceUuid, Guid characteristicUuid, TimeSpan timeSpan)
            => device
                .WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuid)
                .Select(ch => ch.ReadInterval(timeSpan))
                .Switch();


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
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds));



        /// <summary>
        /// Discovers all characteristics for a known service
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetCharacteristicsForService(this IDevice device, Guid serviceUuid) =>
            device
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.DiscoverCharacteristics());


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<IDevice> WhenConnected(this IDevice device) =>
            device
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Connected)
                .Select(_ => device);


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Disconnected)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static IObservable<IDevice> WhenDisconnected(this IDevice device) =>
            device
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Disconnected)
                .Select(_ => device);


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
                .SelectMany(x =>
                {
                    return x.GetKnownCharacteristics(serviceUuid, characteristicIds);
                });


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
