using System;
using System.Reactive.PlatformServices;
using System.Runtime.InteropServices;


namespace Plugin.BluetoothLE
{
    public static partial class CrossBleAdapter
    {
        public static void Init()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new ArgumentException("This platform plugin is only designed to work on Mono/.NET Core using Linux BlueZ");

            Current = new Linux.Adapter();
            AdapterScanner = new Linux.AdapterScanner();
        }
    }
}
