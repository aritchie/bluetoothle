using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Plugin.BluetoothLE.Internals;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly Subject<bool> scanSubject = new Subject<bool>();
        readonly DeviceManager deviceManager = new DeviceManager();


        public override AdapterStatus Status => BluetoothAdapter.IsBluetoothEnabled
            ? AdapterStatus.PoweredOn
            : AdapterStatus.PoweredOff;


        public override IObservable<bool> WhenScanningStatusChanged() => this.scanSubject;


        public override IObservable<IScanResult> Scan(ScanConfig config = null) => Observable.Create<IScanResult>(ob =>
        {
            this.scanSubject.OnNext(true);
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
                this.scanSubject.OnNext(false);
            };
        });


        public override IObservable<AdapterStatus> WhenStatusChanged() => Observable.Create<AdapterStatus>(ob =>
        {
            var handler = new EventHandler<StateChangedEventArgs>((sender, args) => ob.OnNext(this.Status));
            BluetoothAdapter.StateChanged += handler;
            return () => BluetoothAdapter.StateChanged -= handler;
        });
    }
}
