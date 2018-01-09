using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE.Server
{
    public interface IGattServer : IDisposable
    {
        //IBleAdapter Adapter { get; }

        IObservable<IGattService> AddService(Guid uuid, bool primary);
        void RemoveService(Guid serviceUuid);
        void ClearServices();
        IReadOnlyList<IGattService> Services { get; }

        IObservable<CharacteristicSubscription> WhenAnyCharacteristicSubscriptionChanged();
        IList<IDevice> GetAllSubscribedDevices();
    }
}