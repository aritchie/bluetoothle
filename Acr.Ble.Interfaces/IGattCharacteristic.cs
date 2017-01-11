using System;
using System.IO;


namespace Acr.Ble
{
    public interface IGattCharacteristic
    {
        IGattService Service { get; }
        Guid Uuid { get; }
        string Description { get; }
        bool IsNotifying { get; }
        CharacteristicProperties Properties { get; }

        /// <summary>
        /// The current value of the characteristic
        /// </summary>
        byte[] Value { get; }

        /// <summary>
        /// This will (un)subscribe to notifications
        /// </summary>
        IObservable<CharacteristicResult> SubscribeToNotifications();

        /// <summary>
        /// This will only monitor any notifications to the characteristic.  It will not (un)subscribe them.  Use SubscribeToNotifications
        /// </summary>
        /// <returns></returns>
        IObservable<CharacteristicResult> WhenNotificationReceived();

        /// <summary>
        /// Discovers descriptors for this characteristic
        /// </summary>
        /// <returns></returns>
        IObservable<IGattDescriptor> WhenDescriptorDiscovered();

        /// <summary>
        /// This will fire and forget a write
        /// </summary>
        /// <param name="value"></param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        void WriteWithoutResponse(byte[] value, bool reliableWrite = false);

        /// <summary>
        /// Writes the value to the remote characteristic
        /// </summary>
        /// <param name="value">The bytes to send</param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        IObservable<CharacteristicResult> Write(byte[] value, bool reliableWrite = false);

        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="value">The bytes to send</param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        IObservable<BleWriteSegment> BlobWrite(byte[] value, bool reliableWrite = false);

        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="stream">The stream to send</param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        IObservable<BleWriteSegment> BlobWrite(Stream stream, bool reliableWrite = false);

        /// <summary>
        /// Monitor write tasks
        /// </summary>
        /// <returns>Bytes that were successfully written</returns>
        IObservable<CharacteristicResult> WhenWritten();

        /// <summary>
        /// Read characteristic remote value
        /// </summary>
        /// <returns></returns>
        IObservable<CharacteristicResult> Read();

        /// <summary>
        /// Monitors read responses.  Does not send read requests.  Use Read() for that
        /// </summary>
        /// <returns></returns>
        IObservable<CharacteristicResult> WhenRead();
    }
}
