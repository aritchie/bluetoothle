using System;
#if ANDROID
using Android.OS;
using B = Android.OS.Build;
#endif


namespace Plugin.BluetoothLE
{
    public static class CrossBleAdapter
    {

#if __IOS__
        /// <summary>
        /// You should call this before calling BleAdapter.Current!
        /// </summary>
        public static void Init(BleAdapterConfiguration configuration)
        {
            Current = new Adapter(configuration);
        }

#elif ANDROID

        /// <summary>
        /// Specifies the number of Gatt.Connect attempts that will be run before handing off to NativeDevice.ConnectGatt(autoReconnect);
        /// DO NOT CHANGE if you don't know what this is!
        /// </summary>
        public static int AndroidMaxAutoReconnectAttempts { get; set; } = 5;


        /// <summary>
        /// Specifies the wait time before attempting an auto-reconnect
        /// DO NOT CHANGE if you don't know what this is!
        /// </summary>
        public static TimeSpan AndroidPauseBetweenAutoReconnectAttempts { get; set; } = TimeSpan.FromSeconds(1);


        public static bool AndroidMainThreadSuggested =>
            B.VERSION.SdkInt < BuildVersionCodes.Kitkat ||
            B.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase);


        public static bool AndroidPerformActionsOnMainThread { get; set; } = AndroidMainThreadSuggested;

#endif

        static IAdapterScanner scanner;
        public static IAdapterScanner AdapterScanner
        {
            get
            {
#if BAIT
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");
#else
                scanner = scanner ?? new AdapterScanner();
#endif
                return scanner;
            }
            set => scanner = value;
        }


        static IAdapter current;
        public static IAdapter Current
        {
            get
            {
#if BAIT
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");

//#elif WINDOWS_UWP || MONO || MAC
//                if (current == null)
//                    throw new ArgumentException("[Plugin.BluetoothLE] UWP, Mono, & Mac implementations require that you use the CrossBleAdapter.AdapterScanner to set the Current");
#else
                current = current ?? new Adapter();

#endif
                return current;
            }
            set => current = value;
        }
    }
}
