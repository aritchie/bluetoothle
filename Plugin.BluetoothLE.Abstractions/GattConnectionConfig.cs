using System;


namespace Plugin.BluetoothLE
{
    public class GattConnectionConfig
    {
        public static GattConnectionConfig DefaultConfiguration { get; } = new GattConnectionConfig();


        /// <summary>
        /// Set this to false if you want initial connection to be faster (you need to make sure the device is in range)
        /// Set to false if you want to connect when the device comes into range (defaults to true)
        /// </summary>
        public bool AndroidAutoConnect { get; set; } = true;

        /// <summary>
        /// This will cause disconnected devices to try to immediately reconnect.  It will cause WillRestoreState to fire on iOS. Defaults to true
        /// </summary>
        public bool IsPersistent { get;  set; } = true;

        /// <summary>
        /// Android only - If you have characteristics where you need faster replies, you can set this to high
        /// </summary>
        public ConnectionPriority Priority { get; set; } = ConnectionPriority.Normal;
    }
}
