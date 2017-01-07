using System;
using Autofac;
using Acr.Ble;
using Acr.Notifications;


namespace Samples
{
    public class RestoreConnectionNotificationTask : IStartable
    {
        readonly IAdapter adapter;
        readonly INotifications notifications;


        public RestoreConnectionNotificationTask(IAdapter adapter, INotifications notifications)
        {
            this.adapter = adapter;
            this.notifications = notifications;
        }


        public void Start()
        {
            this.adapter
                .WhenDeviceStateRestored()
                .Subscribe(dev => this.notifications.Send(
                    "BLE Restored", 
                    $"Device connection was restored {dev.Name}"
                ));
        }
    }
}
