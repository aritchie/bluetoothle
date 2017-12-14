using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public class AdapterScanner : IAdapterScanner
    {
//#if __MACOS__
//        public bool IsSupported => throw new NotImplementedException();
//        public IObservable<IAdapter> FindAdapters() => throw new NotImplementedException();
//#else
        public bool IsSupported => false;
        public IObservable<IAdapter> FindAdapters() => Observable.Return(CrossBleAdapter.Current);
//#endif
    }
}