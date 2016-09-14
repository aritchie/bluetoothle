using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Acr.Ble;
using Acr.Ble.Plugins;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;
using Xamarin.Forms;
using Command = Acr.Command;


namespace Samples.ViewModels.Le
{
    public class LogViewModel : AbstractViewModel
    {
        readonly IAdapter adapter;
        IDisposable logger;


        public LogViewModel(IAdapter adapter, IAppSettings settings, IAppState appState)
        {
            this.adapter = adapter;
            this.Clear = new Command(() => Device.BeginInvokeOnMainThread(() => this.Output = String.Empty));

            this.IsBackgroundLoggingEnabled = settings.IsBackgroundLoggingEnabled;
            this.WhenAnyValue(x => x.IsBackgroundLoggingEnabled)
                .Skip(1)
                .Subscribe(enabled => settings.IsBackgroundLoggingEnabled = enabled);

            this.WhenAnyValue(x => x.IsForegroundLoggingEnabled)
                .Subscribe(enabled =>
                {
                    if (enabled)
                    {
                        this.logger = this.adapter
                            .WhenActionOccurs(BleLogFlags.All)
                            .Buffer(TimeSpan.FromSeconds(3))
                            .Subscribe(this.Write);
                    }
                    else
                    {
                        this.logger?.Dispose();
                    }
                });

            appState
                .WhenBackgrounding()
                .Subscribe(_ => this.logger?.Dispose());
        }


        public ICommand Clear { get; }
        [Reactive] public bool IsForegroundLoggingEnabled { get; set; }
        [Reactive] public bool IsBackgroundLoggingEnabled { get; set; }
        [Reactive] public string Output { get; private set; }
        // TODO: Save/Share log with another app (ie. Dropbox)
        // TODO: email log


        void Write(IList<string> messages)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var sb = new StringBuilder();
                foreach (var msg in messages)
                {
                    sb.AppendLine($"[{DateTime.Now:T}] {msg}");
                }
                this.Output = sb + this.Output;
            });
        }
    }
}
