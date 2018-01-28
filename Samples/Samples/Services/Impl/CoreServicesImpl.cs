using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;


namespace Samples.Services.Impl
{
    public class CoreServicesImpl : ICoreServices
    {
        public CoreServicesImpl(IUserDialogs dialogs,
                                IViewModelManager vmManager,
                                IAdapter adapter)
        {
            this.Dialogs = dialogs;
            this.VmManager = vmManager;
            this.BleAdapter = adapter;
        }


        public IUserDialogs Dialogs { get; }
        public IViewModelManager VmManager { get; }
        public IAdapter BleAdapter { get; }
    }
}
