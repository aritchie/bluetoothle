using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IAppleGattCharacteristic : IGattCharacteristic
    {
        CBMutableCharacteristic Native { get; }
    }
}
