using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Acr.Ble;
using Acr.Ble.Plugins;
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


        public LogToFileTask(IAdapter adapter, IAppSettings settings, SampleDbConnection data)
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
                            .WhenActionOccurs(BleLogFlags.All)
                            .Buffer(TimeSpan.FromSeconds(5))
                            .Subscribe(this.WriteLog);
                    }
                    else
                    {
                        this.sub?.Dispose();
                    }
                });

        }


        void WriteLog(IList<string> msgs)
        {
            this.data.InsertAll(
                msgs
                    .Select(msg => new BleRecord
                    {
                        Description = msg,
                        TimestampLocal = DateTime.Now
                    }
            ));
        }
    }
}
