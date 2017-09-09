using System;
using System.IO;


namespace Plugin.BluetoothLE
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
        /// Enable notifications (or indications if available)
        /// </summary>
        /// <param name="useIndicationIfAvailable">If true and indication is available, it will be used</param>
        /// <returns></returns>
        IObservable<bool> EnableNotifications(bool useIndicationIfAvailable = false);


        /// <summary>
        /// Disable notifications
        /// </summary>
        IObservable<object> DisableNotifications();


        /// <summary>
        /// This will only monitor any notifications to the characteristic if it is hooked.  It will not (un)subscribe them.  Use SubscribeToNotifications
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
        void WriteWithoutResponse(byte[] value);

        /// <summary>
        /// Writes the value to the remote characteristic
        /// </summary>
        /// <param name="value">The bytes to send</param>
        IObservable<CharacteristicResult> Write(byte[] value);

        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="value">The bytes to send</param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        IObservable<BleWriteSegment> BlobWrite(byte[] value, bool reliableWrite = true);

        /// <summary>
        /// Used for writing blobs
        /// </summary>
        /// <param name="stream">The stream to send</param>
        /// <param name="reliableWrite">Use reliable write atomic writing if available (windows and android)</param>
        IObservable<BleWriteSegment> BlobWrite(Stream stream, bool reliableWrite = true);

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
