using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Characteristic = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public static class Extensions
    {
        public static bool HasNotify(this Characteristic ch) =>
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate) ||
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
    }
}
