using Android.App;
using Android.Bluetooth;
using Android.OS;
using Java.Lang;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Exception = System.Exception;


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
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }


        readonly AutoResetEvent reset = new AutoResetEvent(true);
        public IObservable<T> Lock<T>(IObservable<T> inner)
        {
            if (CrossBleAdapter.AndroidDisableLockMechanism)
                return inner;

            return Observable.Create<T>(ob =>
            {
                IDisposable sub = null;
                var pastGate = false;
                var cancel = false;
                Log.Debug("Device", "Lock - at the gate");

                this.reset.WaitOne();

                if (cancel)
                {
                    Log.Debug("Device", "Lock - past the gate, but was cancelled");
                }
                else
                {
                    pastGate = true;
                    Log.Debug("Device", "Lock - past the gate");

                    if (CrossBleAdapter.AndroidOperationPause != null)
                        System.Threading.Thread.Sleep(CrossBleAdapter.AndroidOperationPause.Value);

                    sub = inner.Subscribe(
                        ob.OnNext,
                        ex =>
                        {
                            Log.Debug("Device", "Task errored - releasing lock");
                            this.reset.Set();
                            pastGate = false;
                            ob.OnError(ex);
                        },
                        () =>
                        {
                            Log.Debug("Device", "Task completed - releasing lock");
                            this.reset.Set();
                            pastGate = false;
                            ob.OnCompleted();
                        }
                    );
                }

                return () =>
                {
                    cancel = true;
                    sub?.Dispose();

                    if (pastGate)
                    {
                        Log.Debug("Device", "Cleanup releasing lock");
                        this.reset.Set();
                    }
                };
            });
        }


        public IObservable<object> Marshall(Action action) => Observable.Create<object>(ob =>
        {
            if (CrossBleAdapter.AndroidPerformActionsOnMainThread)
            {
                if (Application.SynchronizationContext == SynchronizationContext.Current)
                {
                    this.Execute(action, ob);
                }
                else
                {
                    Application.SynchronizationContext.Post(_ => this.Execute(action, ob), null);
                }
            }
            else
            {
                action();
                ob.Respond(null);
            }
            return Disposable.Empty;
        });


        protected virtual void Execute(Action action, IObserver<object> ob)
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
        }


        public IObservable<object> Reconnect(ConnectionPriority priority)
        {
            if (this.Gatt == null)
                throw new ArgumentException("Device is not in a reconnectable state");

            return this.Marshall(() => this.Gatt.Connect());
        }


        public IObservable<object> Connect(ConnectionPriority priority, bool androidAutoReconnect) => this.Marshall(() =>
        {
            this.CreateGatt(androidAutoReconnect);
            if (this.Gatt != null && priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(priority));
        });


        public void Close()
        {
            try
            {
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (Exception ex)
            {
                Log.Warn("Device", "Unclean disconnect - " + ex);
            }
        }


        void CreateGatt(bool autoConnect)
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                var bmMethod = BluetoothAdapter.DefaultAdapter.Class.GetDeclaredMethod("getBluetoothManager");
                bmMethod.Accessible = true;
                var bluetoothManager = bmMethod.Invoke(BluetoothAdapter.DefaultAdapter);

                var method = bluetoothManager.Class.GetDeclaredMethod("getBluetoothGatt");
                method.Accessible = true;
                var iBluetoothGatt = method.Invoke(bluetoothManager);

                if (iBluetoothGatt == null)
                {
                    Log.Debug("Device", "Unable to find getBluetoothGatt object");
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                var bluetoothGatt = this.CreateReflectionGatt(iBluetoothGatt);
                if (bluetoothGatt == null)
                {
                    Log.Info("Device", "Unable to create GATT object via reflection");
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                this.Gatt = bluetoothGatt;
                var connectSuccess = this.ConnectUsingReflection(this.Gatt, true);
                if (!connectSuccess)
                {
                    Log.Error("Device", "Unable to connect using reflection method");
                    this.Gatt?.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Info("Device", "Defaulting to gatt connect compatible method - " + ex);
                this.Gatt = this.ConnectGattCompat(autoConnect);
            }
        }


        bool ConnectUsingReflection(BluetoothGatt bluetoothGatt, bool autoConnect)
        {
            if (autoConnect)
            {
                var autoConnectField = bluetoothGatt.Class.GetDeclaredField("mAutoConnect");
                autoConnectField.Accessible = true;
                autoConnectField.SetBoolean(bluetoothGatt, true);
            }
            var connectMethod = bluetoothGatt.Class.GetDeclaredMethod(
                "connect",
                Java.Lang.Boolean.Type,
                Class.FromType(typeof(BluetoothGattCallback))
            );
            connectMethod.Accessible = true;
            var result = (bool)connectMethod.Invoke(bluetoothGatt, autoConnect, this.Callbacks);
            return result;
        }


        BluetoothGatt CreateReflectionGatt(Java.Lang.Object bluetoothGatt)
        {
            var ctor = Class
                .FromType(typeof(BluetoothGatt))
                .GetDeclaredConstructors()
                .FirstOrDefault();

            ctor.Accessible = true;
            var parms = ctor.GetParameterTypes();
            var args = new Java.Lang.Object[parms.Length];
            for (var i = 0; i < parms.Length; i++)
            {
                var @class = parms[i].CanonicalName.ToLower();
                switch (@class)
                {
                    case "int":
                    case "integer":
                        args[i] = (int)BluetoothTransports.Le;
                        break;

                    case "android.bluetooth.ibluetoothgatt":
                        args[i] = bluetoothGatt;
                        break;

                    case "android.bluetooth.bluetoothdevice":
                        args[i] = this.NativeDevice;
                        break;

                    default:
                        args[i] = Application.Context;
                        break;
                }
            }
            var instance = (BluetoothGatt)ctor.NewInstance(args);
            return instance;
        }

        BluetoothGatt ConnectGattCompat(bool autoConnect) => Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? this.NativeDevice.ConnectGatt(Application.Context, autoConnect, this.Callbacks, BluetoothTransports.Le)
            : this.NativeDevice.ConnectGatt(Application.Context, autoConnect, this.Callbacks);


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

