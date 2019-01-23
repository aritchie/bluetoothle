using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Plugin.BluetoothLE.Server;
using UIKit;
using Foundation;


namespace Plugin.BluetoothLE
{
    public partial class Adapter : AbstractAdapter
    {
        public Adapter(BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(config);
            this.Advertiser = new Advertiser(this);
        }


        public override AdapterFeatures Features => AdapterFeatures.AllServer;

        public override IObservable<IGattServer> CreateGattServer() => Observable.Start(() =>
        {
            var cb = this.context?.PeripheralManager;
            if (cb == null)
                return null;
            WaitForPeripheralManagerIfNeedeed();
            if (!IsPeripheralManagerTurnedOn())
                throw new BleException("Invalid Adapter State - " + cb.State);

            return new GattServer(cb);
        });
    }
}