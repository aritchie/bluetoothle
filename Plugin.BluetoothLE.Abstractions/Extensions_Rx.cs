using System;
using System.Reactive.Subjects;


namespace Plugin.BluetoothLE
{
    public static partial class Extensions
    {

        public static IConnectableObservable<TItem> ReplayWithReset<TItem, TReset>(this IObservable<TItem> src, IObservable<TReset> resetTrigger)
            => new ClearableReplaySubject<TItem, TReset>(src, resetTrigger);


        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }
    }
}
