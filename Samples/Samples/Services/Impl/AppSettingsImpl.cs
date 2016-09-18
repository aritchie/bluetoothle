using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Services.Impl
{
    public class AppSettingsImpl : ReactiveObject, IAppSettings
    {
        [Reactive] public bool AreNotificationsEnabled { get; set; }
        [Reactive] public Guid BleServerServiceUuid { get; set; }
        [Reactive] public bool BleServerEnabled { get; set; }
        [Reactive] public bool IsLoggingEnabled { get; set; }
    }
}
