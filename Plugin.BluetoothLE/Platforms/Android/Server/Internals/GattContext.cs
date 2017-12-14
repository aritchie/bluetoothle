using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server.Internals
{
    public class GattContext
    {
        public BluetoothGattServer Server { get; internal set; }
        public GattServerCallbacks Callbacks { get; } = new GattServerCallbacks();
        public object ServerReadWriteLock { get; } = new object();
    }
}