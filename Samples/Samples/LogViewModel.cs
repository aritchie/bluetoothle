using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Acr.UserDialogs;
using ReactiveUI;
using Samples.Infrastructure;
using Log = Acr.Logging.Log;


namespace Samples.Ble
{
    public class LogItem
    {
        public string Text => $"[{this.Level}/{this.Category}] {this.Timestamp:hh:mm:ss tt}";
        public string Details => this.Message;

        public string Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }


    public class LogViewModel : ViewModel
    {

        public LogViewModel()
        {
            this.Logs = new ObservableCollection<LogItem>();
            this.WhenAnyValue(x => x.Enabled).Subscribe(enable =>
            {
                if (enable)
                {
                    Log.Out = (category, msg, level) =>
                    {
                        // TODO: when in the bg, this will fail
                        //Device.BeginInvokeOnMainThread(() =>
                        //    this.Logs.Insert(0, new LogItem
                        //    {
                        //        Category = category,
                        //        Message = msg,
                        //        Level = level.ToString(),
                        //        Timestamp = DateTime.Now
                        //    })
                        //);
                    };
                }
                else
                {
                    Log.ToConsole();
                }
            });

            this.Show = ReactiveCommand.Create<LogItem>(item => UserDialogs.Instance.Alert(item.Message));
            this.Clear = ReactiveCommand.Create(this.Logs.Clear);
            this.ToggleState = ReactiveCommand.Create(() => this.Enabled = !this.Enabled);
        }


        public string StateText => this.Enabled ? "Disable Logging" : "Enable Logging";


        public override void OnActivated()
        {
            base.OnActivated();
        }


        public override void OnDeactivated()
        {
            base.OnDeactivated();
        }


        bool enabled = true;
        public bool Enabled
        {
            get => this.enabled;
            private set
            {
                this.enabled = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.StateText));
            }
        }


        public ObservableCollection<LogItem> Logs {  get; }
        public ICommand Show { get; }
        public ICommand Clear { get; }
        public ICommand ToggleState { get; }
    }
}
