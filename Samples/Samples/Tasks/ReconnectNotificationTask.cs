using System;
using Autofac;
using Plugin.BluetoothLE;
using Plugin.Notifications;


namespace Samples.Tasks
{
    public class ReconnectNotificationTask : IStartable
    {
        readonly IAdapter adapter;
        readonly INotifications notifications;


        public ReconnectNotificationTask(IAdapter adapter, INotifications notifications)
        {
            this.adapter = adapter;
            this.notifications = notifications;
        }


        public void Start()
        {
        }
    }
}
