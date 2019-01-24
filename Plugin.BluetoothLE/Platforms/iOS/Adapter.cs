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

        public override IObservable<IGattServer> CreateGattServer() => GetPeripheralManagerState().Select(state =>
        {
            if (state != CBPeripheralManagerState.PoweredOn)
                throw new BleException("Invalid Adapter State - " + state);

            return new GattServer(context?.PeripheralManager);
        });
    }
}