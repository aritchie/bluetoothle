using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
using Samples.Infrastructure;
using Xamarin.Forms;
using Log = Plugin.BluetoothLE.Infrastructure.Log;


namespace Samples.Home
{
    public class LogItem
    {
        public string Text => $"[{this.Category}] {this.Message}";
        public string Details => this.Level;

        public string Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
    }

    public class LogViewModel : ViewModel
    {
        public LogViewModel()
        {
            this.Logs = new ObservableCollection<LogItem>();
            this.Clear = ReactiveCommand.Create(() => Device.BeginInvokeOnMainThread(() =>
                this.Logs.Clear()
            ));
        }


        public override void OnActivated()
        {
            base.OnActivated();
            Log.Out = (category, msg, level) => Device.BeginInvokeOnMainThread(() =>
                this.Logs.Insert(0, new LogItem
                {
                    Category = category,
                    Message = msg,
                    Level = level.ToString()
                })
            );
        }


        public override void OnDeactivated()
        {
            base.OnDeactivated();
            Log.ToConsole();
        }


        public ObservableCollection<LogItem> Logs {  get; }
        public ICommand Clear { get; }
    }
}
