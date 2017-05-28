using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;


namespace Samples.Services.Impl
{
    public class CoreServicesImpl : ICoreServices
    {
        public CoreServicesImpl(IUserDialogs dialogs,
                                IAppState state,
                                IViewModelManager vmManager,
                                //IAppSettings appSettings,
                                IAdapter adapter)
        {
            this.Dialogs = dialogs;
            this.AppState = state;
            this.VmManager = vmManager;
            //this.AppSettings = appSettings;
            this.BleAdapter = adapter;
        }


        public IAppState AppState { get; }
        //public IAppSettings AppSettings { get; }
        public IUserDialogs Dialogs { get; }
        public IViewModelManager VmManager { get; }
        public IAdapter BleAdapter { get; }
    }
}
