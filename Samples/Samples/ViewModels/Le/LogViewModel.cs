using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Acr.Ble;
using Acr.Ble.Plugins;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;
using Samples.Services;
using Command = Acr.Command;


namespace Samples.ViewModels.Le
{
    public class LogViewModel : AbstractViewModel
    {
        readonly IAdapter adapter;
        readonly SampleDbConnection data;
        IDisposable logger;


        public LogViewModel(IAdapter adapter, IAppSettings settings, IUserDialogs dialogs, SampleDbConnection data)
        {
            this.adapter = adapter;
            this.data = data;
            this.Clear = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var result = await dialogs.ConfirmAsync(new ConfirmConfig()
                    .SetMessage("Are you sure you wish to delete the log?")
                    .UseYesNo()
                );
                if (result) 
                {                    
                    this.data.DeleteAll<BleRecord>();
                    this.LoadData();
                }
            });

            this.IsLoggingEnabled = settings.IsLoggingEnabled;
      
            this.WhenAnyValue(x => x.IsLoggingEnabled)
                .Skip(1)
                .Subscribe(enabled =>
                {
                    if (enabled)
                    {
                        this.logger = this.adapter
                            .WhenActionOccurs(BleLogFlags.All)
                            .Buffer(TimeSpan.FromSeconds(5))
                            .Where(x => x.Count > 0)
                            .Subscribe(x => this.LoadData());
                    }
                    else
                    {
                        this.logger?.Dispose();
                    }
                });
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.LoadData();
        }


        public override void OnDeactivate()
        {
            base.OnDeactivate();
            this.logger?.Dispose();
        }


        void LoadData()
        {
            this.Data = this.data
                .BleRecords
                .OrderBy(x => x.TimestampLocal)
                .ToList();
        }

        public ICommand Clear { get; }
        [Reactive] public bool IsLoggingEnabled { get; set; }
        [Reactive] public IList<BleRecord> Data { get; private set; }
        // TODO: Save/Share log with another app (ie. Dropbox)
        // TODO: email log
    }
}
