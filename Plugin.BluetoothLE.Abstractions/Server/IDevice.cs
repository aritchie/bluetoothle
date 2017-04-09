using System;


namespace Plugin.BluetoothLE.Server
{
    public interface IDevice
    {
        //string Identifier { get; }
        // I can get this on iOS and Droid
        Guid Uuid { get; }

        /// <summary>
        /// The negotiated MTU size with the remote device
        /// </summary>
        //ushort MtuSize { get; set; }

        /// <summary>
        /// You can set any data you want here
        /// </summary>
        object Context { get; set; }
    }
}
