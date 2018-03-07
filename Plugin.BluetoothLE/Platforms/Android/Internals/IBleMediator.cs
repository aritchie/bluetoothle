using System;


namespace Plugin.BluetoothLE.Internals
{
    public interface IBleMediator
    {
        IObservable<T> Invoke<T>(IDevice device, Action triggerAction, IObservable<T> observable);
    }
}
