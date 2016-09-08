using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Android.App;
using Android.Content;


#if BLE
namespace Acr.Ble.Internals
#else
namespace Acr.Bluetooth.Internals
#endif
{
    public static class AndroidObservables
    {

        public static IObservable<Intent> WhenIntentReceived(string intentAction)
        {
            return Observable.Create<Intent>(ob =>
            {
                var receiver = new ObservableBroadcastReceiver { OnEvent = ob.OnNext };
                Application.Context.RegisterReceiver(receiver, new IntentFilter(intentAction));
                return () => Application.Context.UnregisterReceiver(receiver);
            });
        }
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