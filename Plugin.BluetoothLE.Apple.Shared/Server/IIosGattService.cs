using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IIosGattService : IGattService
    {
        CBMutableService Native { get; }
    }
}
