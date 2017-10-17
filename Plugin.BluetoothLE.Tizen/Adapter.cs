using System;
using System.Reactive.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        public override AdapterStatus Status => BluetoothAdapter.IsBluetoothEnabled
            ? AdapterStatus.PoweredOn
            : AdapterStatus.PoweredOff;


        public override IObservable<bool> WhenScanningStatusChanged() => Observable.Create<bool>(ob =>
        {
            //BluetoothAdapter.DiscoveryStateChanged += (sender, args) =>
            //{
            //};
            return () =>
            {

            };
        });


        public override IObservable<IScanResult> Scan(ScanConfig config = null)
        {
            throw new NotImplementedException();
        }


        public override IObservable<IScanResult> ScanListen() => Observable.Create<IScanResult>(ob =>
        {
            var handler = new EventHandler<AdapterLeScanResultChangedEventArgs>((sender, args) =>
            {
                //args.DeviceData
            });
            BluetoothAdapter.ScanResultChanged += handler;
            BluetoothAdapter.StartLeScan();

            return () =>
            {
                BluetoothAdapter.StopLeScan();
                BluetoothAdapter.ScanResultChanged -= handler;
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
