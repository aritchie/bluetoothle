using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractAdapter : IAdapter
    {
        public abstract IObservable<bool> WhenScanningStatusChanged();
        public abstract IObservable<IScanResult> Scan(ScanConfig config = null);
        public abstract IObservable<AdapterStatus> WhenStatusChanged();

        public virtual string DeviceName { get; protected set; }
        public virtual IGattServer CreateGattServer() => throw new NotImplementedException("CreateGattServer is not supported on this platform");
        public virtual AdapterFeatures Features { get; protected set; }
        public virtual AdapterStatus Status { get; protected set; }
        public virtual bool IsScanning { get; protected set; }
        public virtual IDevice GetKnownDevice(Guid deviceId) => throw new NotImplementedException("GetKnownDevice is not supported on this platform");
        public virtual IEnumerable<IDevice> GetConnectedDevices() => throw new NotImplementedException("GetConnectedDevices is not supported on this platform");
        public virtual IEnumerable<IDevice> GetPairedDevices() => throw new NotImplementedException("GetPairedDevices is not supported on this platform");
        public virtual IObservable<IDevice> WhenDeviceStatusChanged() => throw new NotImplementedException("WhenDeviceStatusChanged is not supported on this platform");
        public virtual void OpenSettings() => throw new NotImplementedException("OpenSettings is not supported on this platform");
        public virtual void SetAdapterState(bool enable) => throw new NotImplementedException("SetAdapterState is not supported on this platform");
        public virtual IObservable<IDevice> WhenDeviceStateRestored() => Observable.Empty<IDevice>();
    }
}
/*
 private readonly string[] Permissions =
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.BluetoothPrivileged,
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);
            Xamarin.Forms.Forms.Init(this, bundle);

            CheckPermissions();

            var app = new App();
            LoadApplication(app);
        }

        private void CheckPermissions()
        {
            bool minimumPermissionsGranted = true;

            foreach (string permission in Permissions)
            {
                if (CheckSelfPermission(permission) != Permission.Granted) minimumPermissionsGranted = false;
            }

            // If one of the minimum permissions aren't granted, we request them from the user
            if (!minimumPermissionsGranted) RequestPermissions(Permissions, 0);
        }
     */