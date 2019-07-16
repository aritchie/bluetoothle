using System;
using System.Threading;
using Android.App;


namespace Plugin.BluetoothLE
{
    static class Platform
    {
        public static void InvokeOnMainThread(Action action)
        {
            if (Application.SynchronizationContext == SynchronizationContext.Current)
                action();
            else
                Application.SynchronizationContext.Post(_ => action(), null);
        }
    }
}
