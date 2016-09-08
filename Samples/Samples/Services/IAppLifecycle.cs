using System;


namespace Samples.Services
{
    public interface IAppLifecycle
    {
        void OnForeground();
        void OnBackground();
    }
}
