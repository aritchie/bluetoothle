//using System;
//using ReactiveUI;
//using ReactiveUI.Fody.Helpers;


//namespace Samples.Services.Impl
//{
//    public class AppSettingsImpl : ReactiveObject, IAppSettings
//    {
//        public AppSettingsImpl()
//        {
//            this.BackgroundScanServiceUuid = new Guid("7c-16-a5-5e-ba-11-cb-92-0c-49-7f-b8-04-11-9a-56".FromHexString());
//        }


//        [Reactive] public bool AreNotificationsEnabled { get; set; }
//        [Reactive] public bool EnableBackgroundScan { get; set; }
//        [Reactive] public Guid BackgroundScanServiceUuid { get; set; }
//        [Reactive] public bool IsLoggingEnabled { get; set; }
//    }
//}
