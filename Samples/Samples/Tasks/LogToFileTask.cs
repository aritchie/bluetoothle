//using System;
//using System.Diagnostics;
//using Plugin.BluetoothLE;
//using Autofac;
//using Samples.Models;
//using Samples.Services;


//namespace Samples.Tasks
//{
//    public class LogToFileTask : IStartable
//    {
//        readonly IAppSettings settings;
//        readonly SampleDbConnection data;


//        public LogToFileTask(IAppSettings settings, SampleDbConnection data)
//        {
//            this.settings = settings;
//            this.data = data;
//        }


//        public void Start()
//        {
//            Log.Out = log =>
//            {
//                if (this.settings.IsLoggingEnabled)
//                {
//                    Debug.WriteLine(log);
//                    this.data.Insert(new BleRecord
//                    {
//                        Description = log,
//                        TimestampLocal = DateTime.Now
//                    });
//                }
//            };
//        }
//    }
//}
