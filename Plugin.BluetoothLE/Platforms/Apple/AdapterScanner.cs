#if __MACOS__
// TODO
using System;

namespace Plugin.BluetoothLE.Platforms.Apple
{
    public class AdapterScanner : IAdapterScanner
    {
        public bool IsSupported => throw new NotImplementedException();
        public IObservable<IAdapter> FindAdapters() => throw new NotImplementedException();
    }
}
#endif