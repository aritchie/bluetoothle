using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Plugin.BluetoothLE.Internals;
using Plugin.BluetoothLE.Server;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly DeviceManager deviceManager = new DeviceManager();


        public override AdapterStatus Status => BluetoothAdapter.IsBluetoothEnabled
            ? AdapterStatus.PoweredOn
            : AdapterStatus.PoweredOff;


        public override IGattServer CreateGattServer() => null;


        public override IObservable<IScanResult> Scan(ScanConfig config = null) => Observable.Create<IScanResult>(ob =>
        {
            this.deviceManager.Clear();
            var handler = new EventHandler<AdapterLeScanResultChangedEventArgs>((sender, args) =>
            {
                var device = this.deviceManager.GetDevice(args.DeviceData);
                ob.OnNext(new ScanResult(args.DeviceData, device));
            });
            BluetoothAdapter.ScanResultChanged += handler;
            BluetoothAdapter.StartLeScan();

            return () =>
            {
                BluetoothAdapter.StopLeScan();
                BluetoothAdapter.ScanResultChanged -= handler;
            };
        });


        public override void StopScan() => BluetoothAdapter.StopLeScan();


        public override IObservable<AdapterStatus> WhenStatusChanged() => Observable.Create<AdapterStatus>(ob =>
        {
            var handler = new EventHandler<StateChangedEventArgs>((sender, args) => ob.OnNext(this.Status));
            BluetoothAdapter.StateChanged += handler;
            return () => BluetoothAdapter.StateChanged -= handler;
        });
    }
}
