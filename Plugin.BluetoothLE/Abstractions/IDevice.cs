using System;


namespace Plugin.BluetoothLE
{

    public interface IDevice
    {
        /// <summary>
        /// Returns the native device instance for external use
        /// </summary>
        object NativeDevice { get; }

        /// <summary>
        /// Contains a flags enum stating what platform features are available
        /// </summary>
        DeviceFeatures Features { get; }

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
        /// <param name="config">Connection configuration</param>
        /// <returns></returns>
        IObservable<object> Connect(GattConnectionConfig config = null);

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
        /// BLE service discovery - This method does not complete.  It will clear all discovered services on subsequent connections
        /// and does not require a connection to hook to it.
        /// </summary>
        IObservable<IGattService> WhenServiceDiscovered();


        /// <summary>
        /// Searches for a known service
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        IObservable<IGattService> GetKnownService(Guid serviceUuid);


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
        /// Make a pairing request
        /// </summary>
        /// <returns></returns>
        IObservable<bool> PairingRequest(string pin = null);

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

        /// <summary>
        /// Begins a reliable write transaction
        /// </summary>
        /// <returns>Transaction session</returns>
        IGattReliableWriteTransaction BeginReliableWriteTransaction();
    }
}