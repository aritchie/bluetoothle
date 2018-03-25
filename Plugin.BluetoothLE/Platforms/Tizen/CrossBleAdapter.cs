using System;


namespace Plugin.BluetoothLE
{
    public static partial class CrossBleAdapter
    {
        static CrossBleAdapter()
        {
            AdapterScanner = new AdapterScanner();
            Current = new Adapter();
        }
    }
}
