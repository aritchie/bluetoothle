using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Android.App;
using Android.Content;


namespace Plugin.BluetoothLE.Internals
{
    public static class AndroidObservables
    {

        public static IObservable<Intent> WhenIntentReceived(string intentAction)
            => Observable.Create<Intent>(ob =>
            {
                var receiver = new ObservableBroadcastReceiver { OnEvent = ob.OnNext };
                Application.Context.RegisterReceiver(receiver, new IntentFilter(intentAction));
                return () => Application.Context.UnregisterReceiver(receiver);
            });
    }


    public class ObservableBroadcastReceiver : BroadcastReceiver
    {
        public Action<Intent> OnEvent { get; set; }

        public override void OnReceive(Context context, Intent intent)
        {
            Debug.WriteLine($"{intent.Action} firing");
            this.OnEvent?.Invoke(intent);
        }
    }
}