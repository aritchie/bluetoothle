//using System;
//using System.Reactive.Linq;
//using Autofac;
//using Plugin.BluetoothLE;


//namespace Samples.Tasks
//{
//    public class NotificationTask : IStartable
//    {
//        readonly IAdapter adapter;
//        readonly INotification notifications;

//        public NotificationTask(IAdapter adapter, INotifications notifications)
//        {
//            this.adapter = adapter;
//            this.notifications = notifications;
//        }


//        public void Start()
//        {
//            this.adapter
//                .WhenStatusChanged()
//                .Skip(1)
//                .Where(x => x == AdapterStatus.PoweredOff)
//                .Subscribe(_ =>
//                {
//                    this.notifications.Send("Turn your bluetooth back on!")
//                });
//        }
//    }
//}
