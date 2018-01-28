using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;


namespace Samples.Services {

    public interface ICoreServices
    {
        IUserDialogs Dialogs { get; }
        IViewModelManager VmManager { get; }
        IAdapter BleAdapter { get; }
    }
}
