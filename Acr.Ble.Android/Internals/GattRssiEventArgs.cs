using System;
using Android.Bluetooth;


namespace Acr.Ble.Internals
{
    public class GattRssiEventArgs : GattEventArgs
    {
        public GattRssiEventArgs(BluetoothGatt gatt, int rssi, GattStatus status) : base(gatt, status)
        {
            this.Rssi = rssi;
        }


        public int Rssi { get; }
    }
}