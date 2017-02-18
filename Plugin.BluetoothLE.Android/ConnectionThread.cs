using System;


namespace Plugin.BluetoothLE
{
    public enum ConnectionThread
    {
        /// <summary>
        /// Allow RX to delegate a thread
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use the main thread (make sure you are sure if you want to use this!)
        /// </summary>
        MainThread = 1,

        /// <summary>
        /// On some flavours of droid, it is suggested that you must connect on the same thread that you scanned the device
        /// </summary>
        ScanThread = 2
    }
}