using Android.App;
using Android.Bluetooth;
using Android.OS;
using Java.Lang;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Plugin.BluetoothLE.Infrastructure;


namespace Plugin.BluetoothLE.Internals
{
    //static TimeSpan? opPause;
    ///// <summary>
    ///// Time span to pause android operations
    ///// DO NOT CHANGE this if you don't know what this is!
    ///// </summary>
    //public static TimeSpan? OperationPause
    //{
    //get
    //{
    //    if (opPause != null)
    //        return opPause;

    //    if (Build.VERSION.SdkInt < BuildVersionCodes.N)
    //        return TimeSpan.FromMilliseconds(100);

    //    return null;
    //}
    //set => opPause = value;
    //}
    public class DeviceContext
    {
        public DeviceContext(BluetoothDevice device, GattCallbacks callbacks)
        {
            this.NativeDevice = device;
            this.Callbacks = callbacks;
            this.Lock = new object();
        }


        public BluetoothGatt Gatt { get; private set; }
        public BluetoothDevice NativeDevice { get; }
        public GattCallbacks Callbacks { get; }
        public object Lock { get; private set; }

        // issues will still arise if the user is doing discovery and data at the same time due to droid
        // TODO: this is botched, reconnect needs to wait?
        public void Reconnect(ConnectionPriority priority)
        {
            if (this.Gatt == null)
                throw new ArgumentException("Device is not in a reconnectable state");

            this.Lock = new object();
            this.InvokeOnMainThread(() => this.Gatt.Connect());
        }


        public void Connect(ConnectionPriority priority, bool androidAutoReconnect) => this.InvokeOnMainThread(() =>
        {
            this.Lock = new object();
            this.CreateGatt(androidAutoReconnect);
            if (this.Gatt != null && priority != ConnectionPriority.Normal)
                this.Gatt.RequestConnectionPriority(this.ToNative(priority));
        });


        public IObservable<T> Invoke<T>(IObservable<T> observable) => Observable.Create<T>(ob =>
            observable.Subscribe(
                ob.OnNext,
                ob.OnError,
                ob.OnCompleted
            )
        )
        .Synchronize(this.Lock);


        public void InvokeOnMainThread(Action action)
        {
            // TODO: should protect the main thread here
            if (AndroidBleConfiguration.ShouldInvokeOnMainThread)
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
                this.Lock = new object();
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (System.Exception ex)
            {
                Log.Warn("Device", "Unclean disconnect - " + ex);
            }
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

