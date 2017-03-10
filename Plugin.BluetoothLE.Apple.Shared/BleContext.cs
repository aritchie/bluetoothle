using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using CoreFoundation;
using Foundation;


namespace Plugin.BluetoothLE
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
//#if __IOS__
            // TODO: RetrieveConnectedPeripherals() async version crashes with bad selector
            // TODO: this method doesn't work
            //return this.Manager
            //    .RetrieveConnectedPeripherals(new CBUUID[0])
            //    .Select(this.GetDevice);
//#else
            return this.peripherals
                .Where(x => 
                    x.Value.Status == ConnectionStatus.Connected || 
                    x.Value.Status == ConnectionStatus.Connecting
                )
                .Select(x => x.Value);
//#endif
        }


        public void Clear()    
        {
            IDevice _;
            this.peripherals
                .Where(x => x.Value.Status != ConnectionStatus.Connected)
                .ToList()
                .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
        }


        public Subject<IDevice> WhenWillRestoreState { get; } = new Subject<IDevice>();
        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
#if __IOS__
            // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
            if (!dict.ContainsKey(CBCentralManager.RestoredStatePeripheralsKey))
                return;

            var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
            Debug.WriteLine($"Restoring peripheral state on {peripheralArray.Count} devices");

            for (nuint i = 0; i < peripheralArray.Count; i++)
            {
                var item = peripheralArray.GetItem<CBPeripheral>(i);
                var dev = this.GetDevice(item);
                this.WhenWillRestoreState.OnNext(dev);
                // TODO: should I trigger any of the device events?
            }
#endif
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