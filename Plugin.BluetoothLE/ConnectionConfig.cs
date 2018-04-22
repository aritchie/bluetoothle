using System;


namespace Plugin.BluetoothLE
{
    public class ConnectionConfig
    {
        /// <summary>
        /// Setting this to false will disable auto connect when the device
        /// is in range or when you disconnect.  However, it will speed up initial
        /// connections signficantly
        /// </summary>
        public bool AndroidAutoConnect { get; set; } = true;


        /// <summary>
        /// Control the android GATT connection priority
        /// </summary>
        public ConnectionPriority AndroidConnectionPriority { get; set; } = ConnectionPriority.Normal;
    }
}
