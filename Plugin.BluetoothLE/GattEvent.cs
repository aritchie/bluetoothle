using System;


namespace Plugin.BluetoothLE
{
    public enum GattEvent
    {
        Read,
        ReadError,
        Write,
        WriteError,
        Notification,
        NotificationError
    }
}
/* Android GattStatus
ConnectionCongested
Failure
InsufficentAuthentication
InsufficientEncryption
InvalidAttributeLength
InvalidOffset
ReadNotPermitted
RequestNotSupported
Success
WriteNotPermitted
 */
