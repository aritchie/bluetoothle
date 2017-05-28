//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Windows.Input;
//using Acr;
//using Plugin.BluetoothLE;
//using Acr.UserDialogs;
//using ReactiveUI;
//using ReactiveUI.Fody.Helpers;
//using Samples.Models;
//using Samples.Services;
//using Command = Acr.Command;


//namespace Samples.ViewModels.Le
//{
//    public class LogViewModel : AbstractViewModel
//    {
//        readonly IAdapter adapter;
//        readonly SampleDbConnection data;
//        IDisposable logger;


//        public LogViewModel(IAdapter adapter, IAppSettings settings, IUserDialogs dialogs, SampleDbConnection data)
//        {
//            this.adapter = adapter;
//            this.data = data;
//            this.Clear = ReactiveCommand.CreateFromTask(async _ =>
//            {
//                var result = await dialogs.ConfirmAsync(new ConfirmConfig()
//                    .SetMessage("Are you sure you wish to delete the log?")
//                    .UseYesNo()
//                );
//                if (result)
//                {
//                    this.data.DeleteAll<BleRecord>();
//                    this.LoadData();
//                }
//            });
//            this.Refresh = new Command(this.LoadData);
//            this.Show = new Command<BleRecord>(rec => dialogs.Alert(rec.Description, "Info"));

//            this.IsLoggingEnabled = settings.IsLoggingEnabled;

//            this.WhenAnyValue(x => x.IsLoggingEnabled)
//                .Skip(1)
//                .Subscribe(enabled =>
//                {
//                    settings.IsLoggingEnabled = enabled;

//                    if (enabled)
//                    {
//                        //this.logger = this.adapter
//                        //    .CreateLogger(BleLogFlags.All)
//                        //    .Buffer(TimeSpan.FromSeconds(5))
//                        //    .Where(x => x.Count > 0)
//                        //    .Subscribe(x => this.LoadData());
//                    }
//                    else
//                    {
//                        this.logger?.Dispose();
//                    }
//                });
//        }


//        public override void OnActivate()
//        {
//            base.OnActivate();
//            this.LoadData();
//        }


//        public override void OnDeactivate()
//        {
//            base.OnDeactivate();
//            this.logger?.Dispose();
//        }


//        void LoadData()
//        {
//            this.IsRefreshing = true;
//            this.Data = this.data
//                .BleRecords
//                .OrderByDescending(x => x.TimestampLocal)
//                .ToList();

//            this.IsRefreshing = false;
//        }

//        public ICommand Clear { get; }
//        public ICommand Refresh { get; }
//        public ICommand Show { get; }
//        [Reactive] public bool IsLoggingEnabled { get; set; }
//        [Reactive] public IList<BleRecord> Data { get; private set; }
//        [Reactive] public bool IsRefreshing { get; private set; }
//        // TODO: Save/Share log with another app (ie. Dropbox)
//        // TODO: email log
//    }
//}
