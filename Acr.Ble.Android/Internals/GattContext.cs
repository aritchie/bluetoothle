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


        public bool Connect(GattConnectionConfig config)
        {
            var success = this.Gatt.Connect();
            if (success && config.Priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(config.Priority));

            return success;
        }


        public void Close()
        {
            this.Gatt.Disconnect();
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Gatt.Close();
        }


        GattConnectionPriority ToNative(ConnectionPriority priority)
        {
            switch (priority)
            {
                case ConnectionPriority.Low:
                    return GattConnectionPriority.LowPower;

                case ConnectionPriority.High:
                    return GattConnectionPriority.High;

                default:
                    return GattConnectionPriority.Balanced;
            }
        }
    }
}

