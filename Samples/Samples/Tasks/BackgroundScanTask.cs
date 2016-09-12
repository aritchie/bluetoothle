using System;
using Acr.Ble;
using Acr.Notifications;
using Samples.Services;


namespace Samples.Tasks
{
    public class BackgroundScanTask : IAppLifecycle
    {
        readonly IAdapter adapter;
        readonly IAppSettings settings;
        readonly INotifications notifications;


        public BackgroundScanTask(IAdapter adapter, IAppSettings settings, INotifications notifications)
        {
            this.adapter = adapter;
            this.settings = settings;
            this.notifications = notifications;
        }


        public void OnForeground()
        {
            this.notifications.Badge = 0;
        }


        public void OnBackground()
        {
            if (!this.settings.BleServerEnabled)
                return;

            this.adapter
                .BackgroundScan(this.settings.BleServerServiceUuid)
                .Subscribe(x =>
                {
                    this.notifications.Badge = this.notifications.Badge + 1;
                    this.notifications.Send("BLE Device Found", $"A device with service UUID {this.settings.BleServerServiceUuid} was found");
                });
        }
    }
}
