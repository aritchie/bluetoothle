using System;


namespace Acr.Ble
{
    public interface IAdapter
    {
        bool IsScanning { get; }
        AdapterStatus Status { get; }

        IObservable<bool> WhenScanningStatusChanged();
        IObservable<IScanResult> Scan();
        //IObservable<IScanResult> BackgroundScan(Guid serviceUuid);
        IObservable<AdapterStatus> WhenStatusChanged();
        IObservable<IDevice> WhenDeviceStatusChanged();
    }
}