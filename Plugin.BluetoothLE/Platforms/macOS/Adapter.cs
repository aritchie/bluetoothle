using System;
using System.Reactive.Linq;
using Plugin.BluetoothLE.Server;


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
        public override IObservable<IGattServer> CreateGattServer()
            => Observable.Return(new GattServer(this.context.PeripheralManager));
	}
}