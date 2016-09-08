using System;
using Acr.Ble;
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
        public IAppSettings AppSettings => this.services.AppSettings;
        public IAdapter BleAdapter => this.services.BleAdapter;
        public ICoreServices Core => this.services;
    }
}
