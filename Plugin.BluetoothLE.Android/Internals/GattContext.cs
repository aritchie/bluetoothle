using System;
using System.Threading;
using Android.App;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class GattContext
    {
        BluetoothGatt gatt;


        public GattContext(BluetoothDevice device, GattCallbacks callbacks)
        {
            this.NativeDevice = device;
            this.Callbacks = callbacks;
        }


        public BluetoothGatt Gatt
        {
            get
            {
                this.gatt = this.gatt ?? this.NativeDevice.ConnectGatt(
                    Application.Context,
                    false,
                    this.Callbacks
                );
                return this.gatt;
            }
        }



        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }


        public void Marshall(Action action)
        {
            if (AndroidConfig.WriteOnMainThread)
            {
                Application.SynchronizationContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }


        public bool Connect(GattConnectionConfig config)
        {
            var success = this.Gatt.Connect();
            if (success && config.Priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(config.Priority));

            return success;
        }


        public void Close()
        {
            this.gatt?.Disconnect();
            this.gatt?.Close();
            this.gatt = null;
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

