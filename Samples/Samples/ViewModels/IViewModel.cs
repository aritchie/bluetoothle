using System;
using System.ComponentModel;
using Acr;

namespace Samples.ViewModels {

    public interface IViewModel : INotifyPropertyChanged
    {
        void Init(object args = null);

        void OnActivate();
        void OnDeactivate();
    }
}
