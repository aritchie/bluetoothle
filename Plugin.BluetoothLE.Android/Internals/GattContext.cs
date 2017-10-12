using System;
using System.Threading;
using System.Threading.Tasks;
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


        public async Task<bool> Connect(GattConnectionConfig config)
        {
            var tcs = new TaskCompletionSource<bool>();
            await this.Marshall(() =>
            {
                var success = this.Gatt.Connect();
                if (success && config.Priority != ConnectionPriority.Normal)
                    this.Gatt.RequestConnectionPriority(this.ToNative(config.Priority));

                tcs.TrySetResult(success);
            });
            return await tcs.Task;
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

