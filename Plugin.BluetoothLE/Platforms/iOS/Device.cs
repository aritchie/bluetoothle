using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    public partial class Device : AbstractDevice
    {
        public override DeviceFeatures Features => DeviceFeatures.MtuRequests;
        public override int MtuSize => (int)this.peripheral.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithResponse);
       
    }
}