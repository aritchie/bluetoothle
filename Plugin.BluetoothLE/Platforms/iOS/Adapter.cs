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
            this.Advertiser = new Advertiser(this.context.PeripheralManager);
        }


        public override AdapterFeatures Features => AdapterFeatures.AllServer;


        public override IObservable<IGattServer> CreateGattServer() => Observable.FromAsync(async ct =>
        {
            var cb = this.context.PeripheralManager;
            if (cb.State != CBPeripheralManagerState.PoweredOn)
            {
                await Task.Delay(3000).ConfigureAwait(false);
                if (cb.State != CBPeripheralManagerState.PoweredOn)
                    throw new BleException("Invalid Adapter State - " + cb.State);
            }

            return new GattServer(cb);
        });
	}
}