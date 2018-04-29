using System;
using Plugin.BluetoothLE;


namespace MvvmCross.Plugin.BluetoothLE
{
    [MvxPlugin]
    [Preserve(AllMembers = true)]
    public class Plugin : IMvxPlugin
    {
        public void Load()
        {
            Mvx.RegisterType(() => CrossBleAdapter.Current);
            Mvx.RegisterType(() => CrossBleAdapter.AdapterScanner);
        }
    }
}
