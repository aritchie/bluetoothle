using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;


namespace Samples.Services {

    public interface ICoreServices
    {
        IAppState AppState { get; }
        //IAppSettings AppSettings { get; }
        IUserDialogs Dialogs { get; }
        IViewModelManager VmManager { get; }
        IAdapter BleAdapter { get; }
    }
}
