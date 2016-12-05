using System;
using System.Threading;
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


        public SemaphoreSlim ReadWriteLock { get; } = new SemaphoreSlim(1, 1);
        public BluetoothGatt Gatt { get; }
        public GattCallbacks Callbacks { get; }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Gatt.Close();
        }
    }
}

