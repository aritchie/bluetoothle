using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;
using Samples.Services;


namespace Samples.ViewModels
{

    public abstract class AbstractRootViewModel : AbstractViewModel
    {
        readonly ICoreServices services;


        protected AbstractRootViewModel(ICoreServices services)
        {
            this.services = services;
        }


        protected IUserDialogs Dialogs => this.services.Dialogs;
        protected IViewModelManager VmManager => this.services.VmManager;
        public IAdapter BleAdapter => this.services.BleAdapter;
    }
}
