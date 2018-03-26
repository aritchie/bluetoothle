//using System;
//using System.Reactive.Linq;
//using Autofac;
//using Plugin.BluetoothLE;
//using Plugin.Notifications;


//namespace Samples.Tasks
//{
//    public class NotificationTask : IStartable
//    {
//        readonly IAdapter adapter;
//        readonly INotifications notifications;

//        public NotificationTask(IAdapter adapter, INotifications notifications)
//        {
//            this.adapter = adapter;
//            this.notifications = notifications;
//        }


//        public void Start() => this.adapter
//            .WhenStatusChanged()
//            .Skip(1)
//            .Where(x => x == AdapterStatus.PoweredOff)
//            .Subscribe(_ => this.notifications.Send(new Notification
//            {
//                Title = "Bluetooth Off",
//                Message = "Turn your bluetooth back on!"
//            }));
//    }
//}
