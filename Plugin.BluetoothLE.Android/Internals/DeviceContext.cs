using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceContext
    {
        public DeviceContext(BluetoothDevice device, GattCallbacks callbacks)
        {
            this.NativeDevice = device;
            this.Callbacks = callbacks;
        }


        public BluetoothGatt Gatt { get; private set; }
        public object SyncLock { get; } = new object();
        //public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }


        public IObservable<object> Marshall(Action action) => Observable.Create<object>(ob =>
        {
            if (CrossBleAdapter.AndroidPerformActionsOnMainThread)
            {
                Application.SynchronizationContext.Post(_ =>
                {
                    try
                    {
                        action();
                        ob.Respond(null);
                    }
                    catch (Exception ex)
                    {
                        ob.OnError(ex);
                    }
                }, null);
            }
            else
            {
                action();
                ob.Respond(null);
            }
            return Disposable.Empty;
        });


        public IObservable<object> Reconnect(ConnectionPriority priority)
        {
            if (this.Gatt == null)
                throw new ArgumentException("Device is not in a reconnectable state");

            return this.Marshall(() => this.Gatt.Connect());
        }


        public IObservable<object> Connect(ConnectionPriority priority, bool androidAutoReconnect) => this.Marshall(() =>
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

