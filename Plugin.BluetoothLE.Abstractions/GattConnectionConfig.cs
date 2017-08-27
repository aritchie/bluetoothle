using System;


namespace Plugin.BluetoothLE
{
    public class GattConnectionConfig
    {
        public static GattConnectionConfig DefaultConfiguration { get; } = new GattConnectionConfig();

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
