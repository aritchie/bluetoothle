using System;
using System.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Characteristic = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public static class UwpExtensions
    {
        public static bool HasNotify(this Characteristic ch) =>
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate) ||
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);


        public static ulong ToMacAddress(this Guid deviceId)
        {
            var address = deviceId
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray();

            var hexAddress = BitConverter.ToString(address).Replace("-", "");
            //if (ulong.TryParse(hexAddress, System.Globalization.NumberStyles.HexNumber, null, out ulong macaddress))

            //    return hexAddress;

            return 0L;
        }
    }
}
