using System;
using Acr.Ble;
using Acr.UserDialogs;


namespace Samples.Services.Impl
{
    public class CoreServicesImpl : ICoreServices
    {
        public CoreServicesImpl(IUserDialogs dialogs,
                                IViewModelManager vmManager,
                                IAppSettings appSettings,
                                IAdapter adapter)
        {
            this.Dialogs = dialogs;
            this.VmManager = vmManager;
            this.AppSettings = appSettings;
            this.BleAdapter = adapter;
        }


        public IAppSettings AppSettings { get; }
        public IUserDialogs Dialogs { get; }
        public IViewModelManager VmManager { get; }
        public IAdapter BleAdapter { get; }
    }
}
