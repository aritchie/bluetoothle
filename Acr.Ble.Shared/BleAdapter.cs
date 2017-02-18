using System;


namespace Plugin.BluetoothLE
{
    public static class BleAdapter
    {

        static readonly Lazy<IAdapter> instanceInit = new Lazy<IAdapter>(() =>
        {
#if PCL
            throw new ArgumentException("[Acr.Ble] No platform plugin found.  Did you install the nuget package in your app project as well?");
#else
            return new Adapter();
#endif
        }, false);


#if __UNIFIED__
        /// <summary>
        /// You should call this before calling BleAdapter.Current!
        /// </summary>
        public static void Init(BleAdapterConfiguration configuration)
        {
            Current = new Adapter(configuration);
        }

#endif


        static IAdapter customInstance;
        public static IAdapter Current
        {
            get { return customInstance ?? instanceInit.Value; }
            set { customInstance = value; }
        }
    }
}
