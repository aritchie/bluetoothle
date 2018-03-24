using System;
using Samples.ViewModels.Le;


namespace Samples.ViewModels
{
    public class MainViewModel : AbstractViewModel
    {
        public MainViewModel(
            ScanViewModel scanViewModel,
            ConnectedDevicesViewModel connectViewModel,
            ServerViewModel serverViewModel)
        {
            this.Scan = scanViewModel;
            this.ConnectedDevices = connectViewModel;
            this.Server = serverViewModel;
        }


        //public LogViewModel Logs { get; }
        public ScanViewModel Scan { get; }
        public ConnectedDevicesViewModel ConnectedDevices { get; }
        public ServerViewModel Server { get; }
    }
}
