using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IIosGattCharacteristic : IGattCharacteristic
    {
        CBMutableCharacteristic Native { get; }
    }
}
