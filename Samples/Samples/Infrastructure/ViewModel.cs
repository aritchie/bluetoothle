using System;
using Acr.XamForms;
using ReactiveUI;


namespace Samples.Infrastructure
{
    public abstract class ViewModel : ReactiveObject, IViewModelLifecycle
    {
        public virtual void OnActivated() {}
        public virtual void OnDeactivated() {}
        public virtual void OnOrientationChanged(bool isPortrait) {}
        public virtual bool OnBackRequested() => true;
        public virtual void OnDestroy() {}
    }
}
