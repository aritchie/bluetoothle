using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server.Internals
{
    public class GattContext
    {
        public GattContext(BluetoothGattServer server)
        {
            this.Server = server;
            this.Callbacks = new GattServerCallbacks();
            this.ServerReadWriteLock = new object();
        }


        public BluetoothGattServer Server { get; }
        public GattServerCallbacks Callbacks { get; }
        public object ServerReadWriteLock { get; }
    }
}