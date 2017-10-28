using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.OS;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceContext
    {
        SemaphoreSlim semaphore;
        int atGate;


        public DeviceContext(BluetoothDevice device, GattCallbacks callbacks)
        {
            this.NativeDevice = device;
            this.Callbacks = callbacks;
            this.SetupSlim();
        }


        public BluetoothGatt Gatt { get; private set; }
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

            this.SetupSlim();
            return this.Marshall(() => this.Gatt.Connect());
        }


        public IObservable<T> LockObservable<T>(Func<IObserver<T>, Task<IDisposable>> action) => Observable.Create<T>(async ob =>
        {
            var gate = false;
            var cts = new CancellationTokenSource();

            this.atGate++;
            Log.Debug("Device", "At gate - " + this.atGate);

            await this.semaphore.WaitAsync(cts.Token);
            this.atGate--;
            Log.Debug("Device", "Past gate - " + this.atGate);

            gate = true;
            var disp = await action(ob);

            return () =>
            {
                disp?.Dispose();
                cts.Cancel();
                if (gate)
                {
                    Log.Debug("Device", "Released gate");
                    this.semaphore.Release();
                }
            };
        });


        public IObservable<object> Connect(ConnectionPriority priority, bool androidAutoReconnect) => this.Marshall(() =>
        {
            this.semaphore?.Dispose();
            this.semaphore = new SemaphoreSlim(1, 1);

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                this.Gatt = this.NativeDevice.ConnectGatt(
                    Application.Context,
                    androidAutoReconnect,
                    this.Callbacks
                );
            }
            //else if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            //{
            //    var transport = BluetoothDevice
            //        .Class
            //        .GetDeclaredField("TRANSPORT_LE")
            //        .GetInt(0);

            //    BluetoothDevice
            //        .Class
            //        .GetDeclaredMethod(
            //            "connectGatt",
            //            Java.Lang.Class.FromType(typeof(Context)),
            //            Java.Lang.Boolean.Type,
            //            Java.Lang.Class.FromType(typeof(BluetoothGattCallback)),
            //            Java.Lang.Integer.Type
            //        )
            //        .Invoke(
            //            this.NativeDevice,
            //            Application.Context,
            //            androidAutoReconnect,
            //            this.Callbacks,
            //            transport
            //        );
            //}
            else
            {
                this.Gatt = this.NativeDevice.ConnectGatt(
                    Application.Context,
                    androidAutoReconnect,
                    this.Callbacks,
                    BluetoothTransports.Le
                );
            }
            if (priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(priority));
        });


        public void Close()
        {
            try
            {
                this.semaphore?.Dispose();
                //this.Gatt?.Disconnect();
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (Exception ex)
            {
                // as long as it closed, do we really care?
                Log.Warn("Device", "Unclean disconnect - " + ex);
            }
        }


        void SetupSlim()
        {
            this.atGate = 0;
            this.semaphore?.Dispose();
            this.semaphore = new SemaphoreSlim(1, 1);
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

