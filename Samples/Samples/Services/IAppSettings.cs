using System;
using System.ComponentModel;


namespace Samples.Services
{
    public interface IAppSettings : INotifyPropertyChanged
    {
        bool AreNotificationsEnabled { get; set; }
        Guid BleServerServiceUuid { get; set; }
        bool BleServerEnabled { get; set; }
        bool IsLoggingEnabled { get; set; }
    }
}
