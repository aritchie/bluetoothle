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


        public BluetoothGatt Gatt { get; private set; }
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }


        public Task Marshall(Action action)
        {
            if (CrossBleAdapter.AndroidPerformActionsOnMainThread)
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


        public Task Reconnect(ConnectionPriority priority)
        {
            if (this.Gatt == null)
                throw new ArgumentException("Device is not in a reconnectable state");

            return this.Marshall(() => this.Gatt.Connect());
        }


        public Task Connect(ConnectionPriority priority, bool androidAutoReconnect) => this.Marshall(() =>
        {
            this.Gatt = this.NativeDevice.ConnectGatt(
                Application.Context,
                androidAutoReconnect,
                this.Callbacks
            );
            if (priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(priority));
        });


        public void Close()
        {
            //this.Gatt?.Disconnect();
            this.Gatt?.Close();
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

