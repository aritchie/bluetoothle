﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Acr.Logging;
using NC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public class DeviceContext
    {
        readonly object syncLock;
        readonly AdapterContext adapterContext;
        readonly IList<GattCharacteristic> subscribers;
        readonly Subject<ConnectionStatus> connSubject;
        readonly Subject<int> mtuSizeSubject;
        readonly ulong bluetoothAddress;


        public DeviceContext(AdapterContext adapterContext,
                             IDevice device,
                             BluetoothLEDevice native)
        {
            this.syncLock = new object();
            this.connSubject = new Subject<ConnectionStatus>();
            this.mtuSizeSubject = new Subject<int>();
            this.currentMtuSize = null;
            this.adapterContext = adapterContext;
            this.subscribers = new List<GattCharacteristic>();
            this.Device = device;
            this.NativeDevice = native;
            this.bluetoothAddress = native.BluetoothAddress;
        }


        public IDevice Device { get; }
        public BluetoothLEDevice NativeDevice { get; private set; }
        public List<GattDeviceService> Services { get; set; } = new List<GattDeviceService>();
        public IObservable<ConnectionStatus> WhenStatusChanged() => this.connSubject.StartWith(this.Status);
        public IObservable<int> WhenMtuChanged() => this.mtuSizeSubject;


        public async Task Connect()
        {
            if (this.NativeDevice != null && this.NativeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                return;

            this.connSubject.OnNext(ConnectionStatus.Connecting);
            this.NativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(this.bluetoothAddress);
            this.NativeDevice.ConnectionStatusChanged += this.OnNativeConnectionStatusChanged;
            var result = await this.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached); // HACK: kick the connection on
            this.Services.AddRange(result.Services);

            // the PDU aka MTU size should be the same on each service instance, nevertheless we determine the minimum value
            this.MtuSize = (int?)this.Services.DefaultIfEmpty(null).Min(service => service?.Session?.MaxPduSize);

            this.connSubject.OnNext(this.Status);
        }


        /// <summary>
        /// Disposes of all references to the <see cref="BluetoothLEDevice"/> object,
        /// which triggers an automatic disconnect after a small timeout period.
        /// </summary>
        /// <references>
        /// <a href="https://stackoverflow.com/a/47708793">Bluetooth LE device cannot disconnect in Win 10 IoT UWP application</a>
        /// <a href="https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/gatt-client#connecting-to-the-device">Bluetooth GATT Client</a>
        /// </references>
        public async Task Disconnect()
        {
            if (this.NativeDevice == null)
                return;

            this.connSubject.OnNext(ConnectionStatus.Disconnecting);
            foreach (var ch in this.subscribers)
            {
                try
                {
                    await ch.Disconnect();
                }
                catch (Exception e)
                {
                    Log.Info(BleLogCategory.Device, "Disconnect Error - " + e);
                }
            }
            this.subscribers.Clear();

            foreach (var service in this.Services)
            {
                service?.Dispose();
            }
            this.Services.Clear();

            this.adapterContext.RemoveDevice(this.NativeDevice.BluetoothAddress);
            this.NativeDevice.ConnectionStatusChanged -= this.OnNativeConnectionStatusChanged;
            this.NativeDevice?.Dispose();
            this.NativeDevice = null;
            this.MtuSize = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        public void SetNotifyCharacteristic(GattCharacteristic characteristic)
        {
            lock (this.syncLock)
            {
                if (characteristic.IsNotifying)
                {
                    this.subscribers.Add(characteristic);
                }
                else
                {
                    this.subscribers.Remove(characteristic);
                }
            }
        }


        public ConnectionStatus Status
        {
            get
            {
                if (this.NativeDevice == null)
                    return ConnectionStatus.Disconnected;

                switch (this.NativeDevice.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        void OnNativeConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            // Connected will be triggered by the Connect method
            if (this.Status == ConnectionStatus.Connected)
                return;

            this.connSubject.OnNext(this.Status);
        }


        int? currentMtuSize;
        public int? MtuSize
        {
            get => currentMtuSize;
            private set
            {
                if (this.currentMtuSize != value)
                {
                    this.currentMtuSize = value;
                    this.mtuSizeSubject.OnNext(value ?? -1);
                }
            }
        }
    }
}