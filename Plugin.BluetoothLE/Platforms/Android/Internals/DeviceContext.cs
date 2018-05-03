using Android.App;
using Android.Bluetooth;
using Android.OS;
using Java.Lang;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Acr.Logging;
using Acr.Reactive;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceContext
    {
        public DeviceContext(BluetoothDevice device)
        {
            this.NativeDevice = device;
            this.Callbacks = new GattCallbacks();
            this.Actions = new ConcurrentQueue<Func<Task>>();
        }


        public BluetoothGatt Gatt { get; private set; }
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }
        public ConcurrentQueue<Func<Task>> Actions { get; }


        public void Connect(ConnectionConfig config) => this.InvokeOnMainThread(() =>
        {
            this.CleanUpQueue();
            this.CreateGatt(config?.AutoConnect ?? true);
            if (this.Gatt == null)
                throw new BleException("GATT connection could not be established");

            var priority = config?.AndroidConnectionPriority ?? ConnectionPriority.Normal;
            if (priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(priority));
        });


        public IObservable<T> Invoke<T>(IObservable<T> observable) => Observable.Create<T>(ob =>
        {
            var cancel = false;
            this.Actions.Enqueue(async () =>
            {
                if (cancel)
                    return;

                try
                {
                    var result = await observable
                        .ToTask()
                        .ConfigureAwait(false);
                    ob.Respond(result);
                }
                catch (System.Exception ex)
                {
                    ob.OnError(ex);
                }
            });
            this.ProcessQueue(); // fire and forget

            return () => cancel = true;
        });


        public async Task OpPause(CancellationToken? cancelToken = null)
        {
            if (CrossBleAdapter.PauseBetweenInvocations != null)
                await Task.Delay(CrossBleAdapter.PauseBetweenInvocations.Value, cancelToken ?? CancellationToken.None);
        }


        public void InvokeOnMainThread(Action action)
        {
            if (CrossBleAdapter.ShouldInvokeOnMainThread)
            {
                if (Application.SynchronizationContext == SynchronizationContext.Current)
                {
                    action();
                }
                else
                {
                    Application.SynchronizationContext.Post(_ => action(), null);
                }
            }
            else
            {
                action();
            }
        }


        public void Close()
        {
            try
            {
                this.CleanUpQueue();
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (System.Exception ex)
            {
                Log.Warn(BleLogCategory.Device, "Unclean disconnect - " + ex);
            }
        }


        bool running;
        async void ProcessQueue()
        {
            if (this.running)
                return;

            this.running = true;
            var ts = CrossBleAdapter.PauseBetweenInvocations;
            while (this.Actions.TryDequeue(out Func<Task> task) && this.running)
            {
                await task();
                if (ts != null)
                    await Task.Delay(ts.Value);
            }
            this.running = false;
        }


        void CleanUpQueue()
        {
            this.running = false;
            this.Actions.Clear();
        }


        void CreateGatt(bool autoConnect)
        {
            try
            {
                // somewhat a copy of android-rxbluetoothle
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
            catch (System.Exception ex)
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

