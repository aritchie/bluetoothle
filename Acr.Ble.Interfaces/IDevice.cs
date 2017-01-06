using System;


namespace Acr.Ble
{

    public interface IDevice
    {
        string Name { get; }
        Guid Uuid { get; }
        ConnectionStatus Status { get; }

        void Disconnect();

        /// <summary>
        /// This is a combination of 3 things - connection management, disconnect (dispose to dc), and state monitoring.  If you don't dispose, reconnect is implied
        /// </summary>
        /// <returns></returns>
        IObservable<ConnectionStatus> CreateConnection();

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


        /// <summary>
        /// The current pairing status
        /// </summary>
        PairingStatus PairingStatus { get; }


        /// <summary>
        /// States whether the API supports pairing or not
        /// </summary>
        bool IsPairingRequestSupported { get; }


        /// <summary>
        /// Make a pairing request
        /// </summary>
        /// <returns></returns>
        IObservable<bool> PairingRequest(string pin = null);


        /// <summary>
        /// If MTU requests are available (Android Only)
        /// This is specific to Android only where this negotiation is not automatic.
        /// The size can be up to 512, but you should be careful with anything above 255 in practice
        /// </summary>
        bool IsMtuRequestAvailable { get; }


        /// <summary>
        /// Send request to set MTU size
        /// </summary>
        /// <param name="size"></param>
        IObservable<int> RequestMtu(int size);

        /// <summary>
        /// Gets the size of the current mtu.
        /// </summary>
        /// <returns>The current mtu size.</returns>
        int GetCurrentMtuSize();

        /// <summary>
        /// Fires when MTU size changes
        /// </summary>
        /// <returns>The mtu change requested.</returns>
        IObservable<int> WhenMtuChanged();
    }
}
//IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
//IObservable<IGattCharacteristic, byte[]> WhenCharacteristicValueChanged();
//IObservable<IGattDescriptor> WhenDescriptorDiscovered();
//IObservable<IGattService> WhenServiceRemoved();
//IObserver<IGattService> WhenServiceInvalidated();