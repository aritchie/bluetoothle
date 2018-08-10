using System;
using System.Reactive.Disposables;
using Acr.XamForms;
using ReactiveUI;


namespace Samples.Infrastructure
{
    public abstract class ViewModel : ReactiveObject, IViewModelLifecycle
    {
        protected CompositeDisposable DeactivateWith { get; private set; }
        protected CompositeDisposable DestroyWith { get; private set; }

        public virtual void OnActivated()
        {
            this.DeactivateWith = new CompositeDisposable();
        }

        public virtual void OnDeactivated()
        {
            this.DeactivateWith?.Dispose();
            if (this.DestroyWith == null)
                this.DestroyWith = new CompositeDisposable();
        }


        public virtual void OnOrientationChanged(bool isPortrait) {}
        public virtual bool OnBackRequested() => true;

        public virtual void OnDestroy()
        {
            this.DestroyWith?.Dispose();
        }
    }
}
