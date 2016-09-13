using System;
using Acr.Ble;
using Acr.Ble.Plugins;
using Autofac;
using ReactiveUI;
using Samples.Services;


namespace Samples.Tasks
{
    public class LogToFileTask : IStartable
    {
        readonly object syncLock = new object();
        readonly IAdapter adapter;
        readonly IAppSettings settings;
        IDisposable sub;


        public LogToFileTask(IAdapter adapter, IAppSettings settings)
        {
            this.adapter = adapter;
            this.settings = settings;
        }


        public void Start()
        {
            this.settings
                .WhenAnyValue(x => x.IsBackgroundLoggingEnabled)
                .Subscribe(doLog =>
                {
                    if (doLog)
                    {
                        this.sub = this.adapter
                            .WhenActionOccurs(BleLogFlags.All)
                            .Subscribe(this.WriteLog);
                    }
                    else
                    {
                        this.sub?.Dispose();
                    }
                });

        }


        void WriteLog(string message)
        {
            var log = $"[{DateTime.Now:T}] {message}";
            lock(this.syncLock)
            {
                // TODO
            }
        }
    }
}
