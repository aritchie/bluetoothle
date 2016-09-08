using System;


namespace Acr.Ble
{

    public interface IDevice
    {
        string Name { get; }
        Guid Uuid { get; }
        ConnectionStatus Status { get; }

        void Disconnect();
        //void RequestMtu();

        /// <summary>
        /// This is a combination of 3 things - smart disconnect (dispose to dc), connection state monitoring, and reconnection
        /// </summary>
        /// <returns></returns>
        IObservable<ConnectionStatus> PersistentConnect();

        /// <summary>
        /// Connect to a device
        /// </summary>
        /// <returns></returns>
        IObservable<object> Connect();

        /// <summary>
        /// Monitor when RSSI updates
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns></returns>
        IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null);


        /// <summary>
        /// Monitor connection status
        /// </summary>
        /// <returns></returns>
        IObservable<ConnectionStatus> WhenStatusChanged();


        /// <summary>
        /// BLE service discovery
        /// </summary>
        /// <returns></returns>
        IObservable<IGattService> WhenServiceDiscovered();


        /// <summary>
        /// Monitor device name changes
        /// </summary>
        /// <returns></returns>
        IObservable<string> WhenNameUpdated();
    }
}
//IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
//IObservable<IGattCharacteristic, byte[]> WhenCharacteristicValueChanged();
//IObservable<IGattDescriptor> WhenDescriptorDiscovered();
//IObservable<IGattService> WhenServiceRemoved();
//IObserver<IGattService> WhenServiceInvalidated();