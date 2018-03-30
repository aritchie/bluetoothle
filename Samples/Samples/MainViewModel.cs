using System;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(ScanViewModel scanViewModel,
                             LogViewModel logs,
                             ServerViewModel serverViewModel)
        {
            this.Scan = scanViewModel;
            this.Logs = logs;
            this.Server = serverViewModel;
        }


        public LogViewModel Logs { get; }
        public ScanViewModel Scan { get; }
        public ServerViewModel Server { get; }
    }
}
