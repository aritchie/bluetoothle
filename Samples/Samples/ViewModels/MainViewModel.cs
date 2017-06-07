using System;
using Samples.ViewModels.Le;
using Samples.ViewModels.TestCases;


namespace Samples.ViewModels
{
    public class MainViewModel : AbstractViewModel
    {
        public MainViewModel(
            ScanViewModel scanViewModel,
            ConnectedDevicesViewModel connectViewModel,
            ServerViewModel serverViewModel,
            TestCasesViewModel testCasesViewModel)
        {
            this.Scan = scanViewModel;
            this.ConnectedDevices = connectViewModel;
            this.Server = serverViewModel;
            this.TestCases = testCasesViewModel;
        }


        //public LogViewModel Logs { get; }
        public ScanViewModel Scan { get; }
        public ConnectedDevicesViewModel ConnectedDevices { get; }
        public ServerViewModel Server { get; }
        public TestCasesViewModel TestCases { get; }
    }
}
