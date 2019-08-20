using System;


namespace Plugin.BluetoothLE
{
    public partial class Device : AbstractDevice
    {
        public override DeviceFeatures Features => DeviceFeatures.None;
        public override int MtuSize { get; } = 20;
        
    }
}