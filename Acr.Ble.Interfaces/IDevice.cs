using System;


namespace Acr.Ble
{

    public interface IDevice
    {
        /// <summary>
        /// The device name - note that this is not readable in the background on most platforms
        /// </summary>
        string Name { get; }


        /// <summary>
        /// The device ID - note that this will not be the same per platform
        /// </summary>
        Guid Uuid { get; }


        /// <summary>
        /// The current connection status
        /// </summary>
        /// <value>The status.</value>
        ConnectionStatus Status { get; }


        /// <summary>
        /// Connect to a device
        /// </summary>
        /// <returns></returns>
        IObservable<object> Connect();


        /// <summary>
        /// Disconnect from the device and cancel persistent connection
        /// </summary>
        void CancelConnection();


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
        /// Gets a service by its known UUID
        /// </summary>
        /// <returns>The service.</returns>
        /// <param name="serviceId">Service identifier.</param>
        IObservable<IGattService> FindServices(params Guid[] serviceId);


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