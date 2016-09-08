using System;


namespace Acr.Ble
{
    public interface IAdapter
    {
        bool IsScanning { get; }
        AdapterStatus Status { get; }

        //bool IsToggleAvailable { get; }
        //Task RequestPermission
        //bool ToggleAdapter(bool power);
        //GetConnectedDevices()
        //IObservable<IScanResult> ScanResults(); - this will only listen to current scans, not start new ones
        // void StopScan

        IObservable<bool> WhenScanningStatusChanged();
        IObservable<IScanResult> Scan(ScanFilter filter = null);
        IObservable<AdapterStatus> WhenStatusChanged();
        IObservable<IDevice> WhenDeviceStatusChanged();
    }
}