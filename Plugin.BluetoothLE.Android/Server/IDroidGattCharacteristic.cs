using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IDroidGattCharacteristic : IGattCharacteristic
    {
        BluetoothGattCharacteristic Native { get; }
    }
}