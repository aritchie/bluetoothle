using System;


namespace Acr.Ble
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


        static IAdapter customInstance;
        public static IAdapter Current
        {
            get { return customInstance ?? instanceInit.Value; }
            set { customInstance = value; }
        }
    }
}
