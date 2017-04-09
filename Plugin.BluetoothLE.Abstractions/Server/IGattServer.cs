using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE.Server
{
    public interface IGattServer : IDisposable
    {
        //IBleAdapter Adapter { get; }
        IObservable<bool> WhenRunningChanged();
        bool IsRunning { get; }
        Task Start(AdvertisementData adData);
        void Stop();

        IGattService AddService(Guid uuid, bool primary);
        void RemoveService(Guid serviceUuid);
        void ClearServices();
        IReadOnlyList<IGattService> Services { get; }

        IObservable<CharacteristicSubscription> WhenAnyCharacteristicSubscriptionChanged();
        IList<IDevice> GetAllSubscribedDevices();
    }
}