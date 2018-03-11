using System;


namespace Plugin.BluetoothLE.Internals
{
    public interface IBleMediator : IDisposable
    {
        IObservable<T> Invoke<T>(IObservable<T> observable);
    }
}
