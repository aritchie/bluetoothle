using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Acr;
using Acr.Ble;
using Acr.Ble.Plugins;
using Acr.UserDialogs;
using Autofac;
using ReactiveUI;
using Samples.Models;
using Samples.Services;


namespace Samples.Tasks
{
    public class LogToFileTask : IStartable
    {
        readonly IAdapter adapter;
        readonly IAppSettings settings;
        readonly SampleDbConnection data;
        IDisposable sub;


        public LogToFileTask(IAdapter adapter, 
                             IAppSettings settings, 
                             IUserDialogs dialogs, 
                             SampleDbConnection data)
        {
            this.adapter = adapter;
            this.settings = settings;
            this.data = data;
        }


        public void Start()
        {
            this.settings
                .WhenAnyValue(x => x.IsLoggingEnabled)
                .Subscribe(doLog =>
                {
                    if (doLog)
                    {
                        this.sub = this.adapter
                            .CreateLogger(BleLogFlags.All)
                            //.CreateLogger(BleLogFlags.CharacteristicRead | BleLogFlags.CharacteristicWrite)
                            .Timestamp()
                            .Buffer(TimeSpan.FromSeconds(5))
                            .Subscribe(this.WriteLog);
                    }
                    else
                    {
                        this.sub?.Dispose();
                    }
                });
        }


        void WriteLog(IList<Timestamped<BleLogEvent>> events)
        {
            this.data.InsertAll(
                events
                    .Select(e => new BleRecord
                    {
                        Description = $"[{e.Value.Category}]({e.Value.Uuid}) {e.Value.Details}",
                        TimestampLocal = e.Timestamp.LocalDateTime
                    }
            ));
        }
    }
}
