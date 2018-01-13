using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Plugin.BluetoothLE.Server.Internals
{
    public class GattContext
    {
        public GattContext()
        {
            this.ServerReadWriteLock = new object();
            this.Callbacks = new GattServerCallbacks();
            this.Manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.Server = this.Manager.OpenGattServer(Application.Context, this.Callbacks);
        }


        public BluetoothManager Manager { get; }
        public BluetoothGattServer Server { get; }
        public GattServerCallbacks Callbacks { get; }
        public object ServerReadWriteLock { get; }
    }
}