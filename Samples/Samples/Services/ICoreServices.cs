using System;
using Acr.Ble;
using Acr.UserDialogs;


namespace Samples.Services {

    public interface ICoreServices
    {
        IAppState AppState { get; }
        IAppSettings AppSettings { get; }
        IUserDialogs Dialogs { get; }
        IViewModelManager VmManager { get; }
        IAdapter BleAdapter { get; }
    }
}
