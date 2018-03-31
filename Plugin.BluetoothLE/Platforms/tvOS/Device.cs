using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    public partial class Device : AbstractDevice
    {

        public override DeviceFeatures Features => DeviceFeatures.MtuRequests;

        public override int MtuSize => (int)this.peripheral.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithResponse);
    }
}