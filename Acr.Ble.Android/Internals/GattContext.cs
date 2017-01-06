using System;
using Android.Bluetooth;


namespace Acr.Ble.Internals
{
    public class GattContext : IDisposable
    {
        public GattContext(BluetoothGatt gatt, GattCallbacks callbacks)
        {
            this.Gatt = gatt;
            this.Callbacks = callbacks;
        }


        public BluetoothGatt Gatt { get; }
        public GattCallbacks Callbacks { get; }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Gatt.Close();
            this.Gatt.Disconnect();
        }
    }
}

