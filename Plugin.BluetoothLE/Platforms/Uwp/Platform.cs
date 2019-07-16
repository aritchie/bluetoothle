using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;


namespace Plugin.BluetoothLE
{
    public static class Platform
    {
        public static void InvokeOnMainThread(Action action)
        {
            //this.dispatcher = dispatcher ?? new Func<Action, Task>(x => CoreApplication
            //                                           +                .MainView
            //    .CoreWindow
            //    .Dispatcher
            //    .RunAsync(CoreDispatcherPriority.Normal, () => x())
            //    .AsTask()
            //);
            CoreApplication.MainView.CoreWindow.Dispatcher?.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
    }
}
