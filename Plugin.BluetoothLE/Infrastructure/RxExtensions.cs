using System;


namespace Acr.Reactive
{
    public static class RxExtensions
    {
        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }
    }
}
