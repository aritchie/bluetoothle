using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IIosGattDescriptor : IGattDescriptor
    {
        CBMutableDescriptor Native { get; }
    }
}
