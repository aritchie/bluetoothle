using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class GattContext
    {
        public GattContext(BluetoothDevice device, GattCallbacks callbacks)
        {
            this.NativeDevice = device;
            this.Callbacks = callbacks;
        }


        //BluetoothGatt gatt;
        public BluetoothGatt Gatt { get; private set; }
        //{
        //    get
        //    {
        //        this.gatt = this.gatt ?? this.NativeDevice.ConnectGatt(
        //            Application.Context,
        //            false,
        //            this.Callbacks
        //        );
        //        return this.gatt;
        //    }
        //}



        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }


        public Task Marshall(Action action)
        {
            if (AndroidConfig.PerformActionsOnMainThread)
            {
                var tcs = new TaskCompletionSource<object>();
                Application.SynchronizationContext.Post(_ =>
                {
                    try
                    {
                        action();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }, null);
                return tcs.Task;
            }
            action();
            return Task.CompletedTask;
        }


        public Task Connect(GattConnectionConfig config) => this.Marshall(() =>
        {
            this.Gatt = this.NativeDevice.ConnectGatt(
                Application.Context,
                true,
                this.Callbacks
            );
            if (config.Priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(config.Priority));
        });


        public void Close()
        {
            this.Gatt?.Disconnect();
            this.Gatt = null;
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

