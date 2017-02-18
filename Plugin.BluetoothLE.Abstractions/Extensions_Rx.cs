using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {

        public static IConnectableObservable<TItem> ReplayWithReset<TItem, TReset>(this IObservable<TItem> src, IObservable<TReset> resetTrigger)
        {
            return new ClearableReplaySubject<TItem, TReset>(src, resetTrigger);
        }


        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }
    }
}
