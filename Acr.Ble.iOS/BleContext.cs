using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using CoreFoundation;
using Foundation;


namespace Acr.Ble
{
    public class BleContext : CBCentralManagerDelegate
    {
        readonly ConcurrentDictionary<string, IDevice> peripherals = new ConcurrentDictionary<string, IDevice>();

        public BleContext(BleAdapterConfiguration config)
        {
            this.Manager = config == null
                ? new CBCentralManager(this, DispatchQueue.CurrentQueue)
                : new CBCentralManager(this, config.DispatchQueue, new CBCentralInitOptions
                {
                    ShowPowerAlert = config.ShowPowerAlert,
#if __IOS__
                    RestoreIdentifier = config.RestoreIdentifier
#endif
                });
        }


        public CBCentralManager Manager { get; }


        public IDevice GetDevice(CBPeripheral peripheral)
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
            ////if (!dict.ContainsKey(CBCentralManager.RestoredStatePeripheralsKey))
            ////    return;

            ////var items = dict[CBCentralManager.RestoredStatePeripheralsKey];
            ////Debug.WriteLine("[WillRestoreState]: " + items.GetType().FullName);
            // TODO: restore scanning?  CBCentralManager.RestoredStateScanOptionsKey
            //this.GetDevice(null)
        }


        public Subject<CBPeripheral> PeripheralConnected { get; } = new Subject<CBPeripheral>();
        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            this.PeripheralConnected.OnNext(peripheral);
        }


        public Subject<CBPeripheral> PeripheralDisconnected { get; } = new Subject<CBPeripheral>();
        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            this.PeripheralDisconnected.OnNext(peripheral);
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


        public Subject<PeripheralConnectionFailed> FailedConnection { get; } = new Subject<PeripheralConnectionFailed>();
        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            this.FailedConnection.OnNext(new PeripheralConnectionFailed(peripheral, error));
        }


        public Subject<object> StateUpdated { get; } = new Subject<object>();
        public override void UpdatedState(CBCentralManager central)
        {
            this.StateUpdated.OnNext(null);
        }
    }
}