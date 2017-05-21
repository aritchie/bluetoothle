using System;


namespace Plugin.BluetoothLE
{
    public interface IAdapterScanner
    {
        bool IsSupported { get; }
        IObservable<IAdapter> FindAdapters();
    }
}
