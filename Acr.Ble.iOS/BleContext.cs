using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;


namespace Acr.Ble
{
    public class BleContext : CBCentralManagerDelegate
    {
        readonly ConcurrentDictionary<string, IDevice> peripherals = new ConcurrentDictionary<string, IDevice>();

        public BleContext()
        {
            //this.Manager = new CBCentralManager(DispatchQueue.GetGlobalQueue(DispatchQueuePriority.Background));
            this.Manager = new CBCentralManager(this, null, new CBCentralInitOptions
            {
                ShowPowerAlert = false,
                RestoreIdentifier = "acr.ble"
            });
        }


        public CBCentralManager Manager { get; }


        IDevice GetDevice(CBPeripheral peripheral)
        {
            return this.peripherals.GetOrAdd(
                peripheral.Identifier.ToString(),
                x => new Device(this, peripheral)
            );
        }


        public IEnumerable<IDevice> GetConnectedDevices()
        {
            return this.peripherals
                .Where(x => x.Value.Status == ConnectionStatus.Connected)
                .Select(x => x.Value)
                .ToList();
        }


        public void Clear()
        {
            IDevice _;
            this.peripherals
                .Where(x => x.Value.Status != ConnectionStatus.Connected)
                .ToList()
                .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
        }


        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
        }


        // TODO: need to filter to native periperhal
        public Subject<IDevice> DeviceConnected { get; } = new Subject<IDevice>();
        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            var dev = this.GetDevice(peripheral);
            this.DeviceConnected.OnNext(dev);
        }


        // TODO: need to filter to native periperhal
        public Subject<IDevice> DeviceDisconnected { get; } = new Subject<IDevice>();
        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            var dev = this.GetDevice(peripheral);
            this.DeviceDisconnected.OnNext(dev);
        }


        public Subject<ScanResult> ScanResultReceived { get; } = new Subject<ScanResult>();
        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
        {
            var dev = this.GetDevice(peripheral);
            this.ScanResultReceived.OnNext(new ScanResult(
                dev,
                rssi?.Int32Value ?? 0,
                new AdvertisementData(advertisementData)
            ));
        }


        // TODO: need to filter to native periperhal
        public Subject<IDevice> FailedConnection { get; } = new Subject<IDevice>();
        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            var dev = this.GetDevice(peripheral);
            this.FailedConnection.OnNext(dev);
        }


        public Subject<object> StateUpdated { get; } = new Subject<object>();
        public override void UpdatedState(CBCentralManager central)
        {
            this.StateUpdated.OnNext(null);
        }
    }
}