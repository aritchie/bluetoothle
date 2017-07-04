using System;


namespace Plugin.BluetoothLE
{
    public class AdapterScanner : IAdapterScanner
    {
        public bool IsSupported { get; }
        public IObservable<IAdapter> FindAdapters()
        {
            throw new NotImplementedException();
        }
    }
}
