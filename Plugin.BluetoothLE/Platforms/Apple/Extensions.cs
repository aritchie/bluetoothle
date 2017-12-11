using System;
using Foundation;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    static class BleExtensions
    {
        public static Guid ToGuid(this NSUuid uuid) => Guid.ParseExact(uuid.AsString(), "d");

        public static Guid ToGuid(this CBUUID uuid)
        {
            var id = uuid.ToString();
            if (id.Length == 4)
                id = $"0000{id}-0000-1000-8000-00805f9b34fb";

            return Guid.ParseExact(id, "d");
        }


        public static CBUUID ToCBUuid(this Guid guid) => CBUUID.FromString(guid.ToString());
        public static NSUuid ToNSUuid(this Guid guid) => new NSUuid(guid.ToString());


        public static CBCharacteristicProperties ToNative(this CharacteristicProperties properties)
        {
            var nativeProps = CBCharacteristicProperties.Read;

            if (!properties.HasFlag(CharacteristicProperties.Read))
                nativeProps &= ~CBCharacteristicProperties.Read;
            
            if (properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites))
                nativeProps |= CBCharacteristicProperties.AuthenticatedSignedWrites;

            if (properties.HasFlag(CharacteristicProperties.Broadcast))
                nativeProps |= CBCharacteristicProperties.Broadcast;

            if (properties.HasFlag(CharacteristicProperties.ExtendedProperties))
                nativeProps |= CBCharacteristicProperties.ExtendedProperties;

            if (properties.HasFlag(CharacteristicProperties.Indicate))
                nativeProps |= CBCharacteristicProperties.Indicate;

            if (properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired))
                nativeProps |= CBCharacteristicProperties.IndicateEncryptionRequired;

            if (properties.HasFlag(CharacteristicProperties.Notify))
                nativeProps |= CBCharacteristicProperties.Notify;

            if (properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired))
                nativeProps |= CBCharacteristicProperties.NotifyEncryptionRequired;

            if (properties.HasFlag(CharacteristicProperties.Write))
                nativeProps |= CBCharacteristicProperties.Write;

            if (properties.HasFlag(CharacteristicProperties.WriteNoResponse))
                nativeProps |= CBCharacteristicProperties.WriteWithoutResponse;

            return nativeProps;
        }
    }
}