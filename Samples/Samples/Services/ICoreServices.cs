using System;
using System.Threading.Tasks;
using Acr.Ble;
using Acr.UserDialogs;


namespace Samples.Services {

    public interface ICoreServices
    {
        IAppSettings AppSettings { get; }
        IUserDialogs Dialogs { get; }
        IViewModelManager VmManager { get; }
        IAdapter BleAdapter { get; }
    }
}
