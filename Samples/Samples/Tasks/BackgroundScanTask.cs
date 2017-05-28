//using System;
//using System.Diagnostics;
//using Plugin.BluetoothLE;
//using Acr.Notifications;
//using Samples.Services;


//namespace Samples.Tasks
//{
//    public class BackgroundScanTask : IAppLifecycle
//    {
//        readonly IAdapter adapter;
//        readonly IAppSettings settings;
//        readonly INotifications notifications;
//        IDisposable bgScan;


//        public BackgroundScanTask(IAdapter adapter, IAppSettings settings, INotifications notifications)
//        {
//            this.adapter = adapter;
//            this.settings = settings;
//            this.notifications = notifications;
//        }


//        public void OnForeground()
//        {
//            this.notifications.Badge = 0;
//            this.bgScan?.Dispose();
//        }


//        public void OnBackground()
//        {
//            if (!this.settings.EnableBackgroundScan)
//                return;

//            Debug.WriteLine("Starting Background Scan");
//            this.bgScan = this.adapter
//                .Scan(new ScanConfig
//                {
//                    ScanType = BleScanType.Background,
//                    ServiceUuid = this.settings.BackgroundScanServiceUuid
//                })
//                .Subscribe(x =>
//                {
//                    Debug.WriteLine($"[background] {x.Device.Name} - {x.Device.Uuid}");
//                    this.notifications.Badge = this.notifications.Badge + 1;
//                    this.notifications.Send("BLE Device Found", $"A device was found");
//                },
//                ex => Debug.WriteLine("Background Scan Error - " + ex),
//                ()=> Debug.WriteLine("Killing Background Scan")
//            );
//        }
//    }
//}
