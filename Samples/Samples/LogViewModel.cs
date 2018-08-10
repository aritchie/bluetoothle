using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.Collections;
using Acr.UserDialogs;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class LogViewModel : ViewModel
    {
        readonly ILogService logs;


        public LogViewModel()
        {
            this.logs = LogService.Instance;
            this.Show = ReactiveCommand.Create<LogItem>(item => UserDialogs.Instance.Alert(item.Message));
            this.Clear = ReactiveCommand.Create(this.logs.Clear);
        }


        public override void OnActivated()
        {
            base.OnActivated();
            var l = this.logs.GetLogs();
            this.Logs.Clear();

            if (l.Any())
                this.Logs.AddRange(l);

            this.logs
                .WhenUpdated()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                    this.Logs.Insert(0, x)
                )
                .DisposeWith(this.DeactivateWith);
        }


        public ObservableList<LogItem> Logs { get; } = new ObservableList<LogItem>();
        public ICommand Show { get; }
        public ICommand Clear { get; }
    }
}
