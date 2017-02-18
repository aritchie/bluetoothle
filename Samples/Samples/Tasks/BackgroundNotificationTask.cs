using System;
using Autofac;
using Plugin.BluetoothLE;
using Acr.Notifications;
using Samples.Services;
using Xamarin.Forms;

namespace Samples.Tasks
{
    public class BackgroundNotificationTask : IStartable, IAppLifecycle
    {
        readonly IAdapter adapter;
        readonly INotifications notifications;
        IDisposable deviceStateOb;


        public BackgroundNotificationTask(IAdapter adapter, INotifications notifications)
        {
            this.adapter = adapter;
            this.notifications = notifications;
        }


        public void OnBackground()
        {
            this.deviceStateOb = this.adapter
                .WhenDeviceStatusChanged()
                .Subscribe(dev => Device.BeginInvokeOnMainThread(() => this.notifications.Send(
                    "BLE Device Status", 
                    $"Device connection changed for {dev.Name} to {dev.Status}"
                )));
        }


        public void OnForeground()
        {
            this.deviceStateOb?.Dispose();
        }


        public void Start()
        {
            this.adapter
                .WhenDeviceStateRestored()
                .Subscribe(dev => Device.BeginInvokeOnMainThread(() => this.notifications.Send(
                    "BLE Restored", 
                    $"Device connection was restored {dev.Name}"
                )));
        }

    }
}
