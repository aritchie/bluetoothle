using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public class AdapterScannerImpl : IAdapterScanner
    {
        public IObservable<IAdapter> FindAdapters() => Observable.Empty<IAdapter>();
    }
}
