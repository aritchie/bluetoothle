using System;


namespace Plugin.BluetoothLE
{
    public interface IAdapterScanner
    {
        IObservable<IAdapter> FindAdapters();
    }
}
