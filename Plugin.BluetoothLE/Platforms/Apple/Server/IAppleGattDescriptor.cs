using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IAppleGattDescriptor : IGattDescriptor
    {
        CBMutableDescriptor Native { get; }
    }
}
