using System;
using System.Collections;
using System.Collections.Generic;


namespace Acr.Ble
{
    public interface IAdapter
    {
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
        /// Monitor for scanning status changes
        /// </summary>
        /// <returns></returns>
        IObservable<bool> WhenScanningStatusChanged();

        /// <summary>
        /// Start scanning for BluetoothLE devices
        /// </summary>
        /// <returns></returns>
        IObservable<IScanResult> Scan();

        /// <summary>
        /// Scan for BluetoothLE background services
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        IObservable<IScanResult> BackgroundScan(Guid serviceUuid);

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
        /// <returns><c>true</c>, if settings was opened, <c>false</c> otherwise.</returns>
        bool OpenSettings();
    }
}