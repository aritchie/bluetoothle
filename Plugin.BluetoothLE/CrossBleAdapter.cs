using System;


namespace Plugin.BluetoothLE
{
    public static partial class CrossBleAdapter
    {
        static IAdapterScanner scanner;
        public static IAdapterScanner AdapterScanner
        {
            get
            {
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");

                return scanner;
            }
            set => scanner = value;
        }


        static IAdapter current;
        public static IAdapter Current
        {
            get
            {
                if (current == null)
                    throw new ArgumentException("[Plugin.BluetoothLE] No platform plugin found.  Did you install the nuget package in your app project as well?");

                return current;
            }
            set => current = value;
        }


        static AndroidConfig config;
        public static AndroidConfig AndroidConfiguration
        {
            get
            {
                config = config ?? new AndroidConfig();
                return config;
            }
        }
    }
}
