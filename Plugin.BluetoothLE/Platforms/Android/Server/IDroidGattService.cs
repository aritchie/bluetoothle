using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IDroidGattService : IGattService
    {
        BluetoothGattService Native { get; }
    }
}