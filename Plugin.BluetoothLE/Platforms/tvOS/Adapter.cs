using System;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public partial class Adapter : AbstractAdapter
    {
        public Adapter(BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(config);
        }


        public override AdapterFeatures Features => AdapterFeatures.None;
        public override IGattServer CreateGattServer()
        {
            throw new Exception("GATT Servers are not supported on tvOS");
        }

        public override bool IsScanning => this.context.Manager.IsScanning;
	}
}