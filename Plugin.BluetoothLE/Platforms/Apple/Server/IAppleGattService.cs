using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IAppleGattService : IGattService
    {
        CBMutableService Native { get; }
    }
}
