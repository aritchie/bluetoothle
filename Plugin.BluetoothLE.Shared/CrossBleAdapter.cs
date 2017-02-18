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


        static IAdapter current;
        public static IAdapter Current
        {
            get
            {
#if BAIT
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");

                return current;
#else
                current = current ?? new Adapter();
                return current;
#endif
            }
            set { current = value; }
        }
    }
}
