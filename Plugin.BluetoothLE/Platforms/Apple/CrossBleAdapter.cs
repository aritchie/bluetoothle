using System;


namespace Plugin.BluetoothLE
{
    public static partial class CrossBleAdapter
    {
        static CrossBleAdapter()
        {
            Current = new Adapter();
#if __MACOS__
            AdapterScanner = new AdapterScanner();
#else
            AdapterScanner = new NotSupportedAdapterScanner();
#endif
        }


        #if __IOS__

        /// <summary>
        /// You should call this before calling BleAdapter.Current!
        /// </summary>
        public static void Init(BleAdapterConfiguration configuration = null)
        {
            Current = new Adapter(configuration);
        }

        #endif
    }
}
