using System;
using System.ComponentModel;
using Acr;

namespace Samples.ViewModels {

    public interface IViewModel : INotifyPropertyChanged, IViewModelLifecycle
    {
        void Init(object args = null);
    }
}
