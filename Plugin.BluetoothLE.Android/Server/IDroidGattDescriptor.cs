using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server
{
    public interface IDroidGattDescriptor : IGattDescriptor
    {
        BluetoothGattDescriptor Native { get; }
    }
}