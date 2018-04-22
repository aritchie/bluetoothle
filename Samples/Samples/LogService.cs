using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;


namespace Samples
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


    public interface ILogService
    {
        IList<LogItem> GetLogs();
        void Clear();

        IObservable<LogItem> WhenUpdated();
    }


    public class LogService : ILogService
    {
        readonly object syncLock = new object();
        readonly IList<LogItem> items = new List<LogItem>();
        readonly Subject<LogItem> logSubject = new Subject<LogItem>();

        public static ILogService Instance { get; } = new LogService();


        public LogService()
        {
            Acr.Logging.Log.Out = (category, message, level) =>
            {
                Debug.WriteLine($"");
                lock (this.syncLock)
                {
                    var item = new LogItem
                    {
                        Category = category,
                        Message = message,
                        Level = level.ToString(),
                        Timestamp = DateTime.Now
                    };
                    this.items.Insert(0, item);
                    this.logSubject.OnNext(item);
                }
            };
        }


        public IList<LogItem> GetLogs()
        {
            lock (this.syncLock)
                return this.items.ToList();
        }


        public void Clear()
        {
            lock (this.syncLock)
                this.items.Clear();
        }


        public IObservable<LogItem> WhenUpdated() => this.logSubject;
    }
}
