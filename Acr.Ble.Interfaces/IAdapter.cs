using System;
using System.Collections.Generic;


namespace Plugin.BluetoothLE
{
    public interface IAdapter
    {
        /// <summary>
        /// This readonly property contains a flags enum stating what platform adapter features that are available
        /// </summary>
        AdapterFeatures Features { get; }

        /// <summary>
        /// Get a known device
        /// </summary>
        /// <param name="deviceId">Device identifier.</param>
        IDevice GetKnownDevice(Guid deviceId);

        /// <summary>
        /// Returns current status of adapter (on/off/permission)
        /// </summary>
        AdapterStatus Status { get; }

        /// <summary>
        /// Get current scanning status
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// Gets a list of connected devices
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDevice> GetConnectedDevices();

        /// <summary>
        /// Get the list of paired devices
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDevice> GetPairedDevices();

            /// <summary>
        /// Monitor for scanning status changes
        /// </summary>
        /// <returns></returns>
        IObservable<bool> WhenScanningStatusChanged();

        /// <summary>
        /// Start scanning for BluetoothLE devices
        /// WARNING: only one scan can be active at a time.  Use ScanListen to listen in on existing scans.  Use IsScanning to check for active scanning
        /// </summary>
        /// <returns></returns>
        IObservable<IScanResult> Scan(ScanConfig config = null);

        /// <summary>
        /// Allows you to listen in on current scans in progress (usualful for background tasks like logging and decoupled components)
        /// </summary>
        /// <returns></returns>
        IObservable<IScanResult> ScanListen();

        /// <summary>
        /// Monitor for status changes with adapter (on/off/permissions)
        /// </summary>
        /// <returns></returns>
        IObservable<AdapterStatus> WhenStatusChanged();

        /// <summary>
        /// Monitor for all device status changes
        /// </summary>
        /// <returns></returns>
        IObservable<IDevice> WhenDeviceStatusChanged();

        /// <summary>
        /// Opens the platform settings screen
        /// </summary>
        void OpenSettings();

        /// <summary>
        /// Toggles the bluetooth adapter on/off - returns true if successful
        /// Works only on Android
        /// </summary>
        /// <returns></returns>
        void SetAdapterState(bool enable);

        /// <summary>
        /// iOS ONLY - this is called for WillRestoreState is performed
        /// You must use BleAdapter.Init in your iOS project to set the configuration options
        /// </summary>
        /// <returns></returns>
        IObservable<IDevice> WhenDeviceStateRestored();
    }
}