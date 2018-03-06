using System;


namespace Plugin.BluetoothLE.Internals
{
    public interface IBleInvoker
    {
        IObservable<T> Invoke<T>(Action triggerAction, IObservable<T> observable);
    }
}
