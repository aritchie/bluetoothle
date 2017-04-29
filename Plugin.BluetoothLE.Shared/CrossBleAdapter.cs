using System;


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

#endif

        static IAdapterScanner scanner;

        /// <summary>
        /// Only supported on UWP - will be null on other platforms
        /// </summary>
        public static IAdapterScanner AdapterScanner
        {
            get
            {
#if WINDOWS_UWP || MAC
                scanner = scanner ?? new AdapterScanner();
#endif
                return scanner;
            }
            set { scanner = value; }
        }


        static IAdapter current;
        public static IAdapter Current
        {
            get
            {
#if BAIT
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");

#elif WINDOWS_UWP || MONO || MAC
//#elif WINDOWS_UWP
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] UWP requires that you use the CrossBleAdapter.AdapterScanner to set the Current");
                    //throw new ArgumentException("[Plugin.BluetoothLE] UWP, Mono, & Mac implementations require that you use the CrossBleAdapter.AdapterScanner to set the Current");
#else
                current = current ?? new Adapter();

#endif
                return current;
            }
            set { current = value; }
        }
    }
}
