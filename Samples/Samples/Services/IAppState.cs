using System;


namespace Samples.Services
{
    public interface IAppState
    {
        IObservable<object> WhenBackgrounding();
        IObservable<object> WhenResuming();
    }
}
