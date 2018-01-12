using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE.Server
{
    public interface IGattServer : IDisposable
    {
        /// <summary>
        /// Creates a service - does not add it to the server.  Use AddService!
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="primary"></param>
        /// <returns></returns>
        IGattService CreateService(Guid uuid, bool primary);

        /// <summary>
        /// Will add the service created by CreateService to the server for you - make sure to add your
        /// </summary>
        /// <param name="service"></param>
        void AddService(IGattService service);

        /// <summary>
        /// This will Create & add the service to the server - make sure to add your characteristics and descriptors in the callback
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="primary"></param>
        /// <param name="callback"></param>
        void AddService(Guid uuid, bool primary, Action<IGattService> callback);

        void RemoveService(Guid serviceUuid);
        void ClearServices();
        IReadOnlyList<IGattService> Services { get; }

        IObservable<CharacteristicSubscription> WhenAnyCharacteristicSubscriptionChanged();
        IList<IDevice> GetAllSubscribedDevices();
    }
}