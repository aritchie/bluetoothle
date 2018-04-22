using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server.Internals
{
    public class CharacteristicReadEventArgs : GattRequestEventArgs
    {
        public CharacteristicReadEventArgs(
            BluetoothDevice device,
            BluetoothGattCharacteristic characteristic,
            int requestId,
            int offset) : base(device, requestId, offset)
        {
            this.Characteristic = characteristic;
        }


        public BluetoothGattCharacteristic Characteristic { get; }
    }
}