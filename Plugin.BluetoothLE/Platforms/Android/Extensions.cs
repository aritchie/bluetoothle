using System;
using Android.Bluetooth;
using Android.OS;
using Java.Util;
using Plugin.BluetoothLE.Server;
using DroidGattStatus = Android.Bluetooth.GattStatus;
using GattStatus = Plugin.BluetoothLE.Server.GattStatus;


namespace Plugin.BluetoothLE
{
    public static class Extensions
    {
        public static Guid ToGuid(this byte[] uuidBytes)
        {
            Array.Reverse(uuidBytes);
            var id = BitConverter
                .ToString(uuidBytes)
                .Replace("-", String.Empty);

            switch (id.Length)
            {
                case 4:
                    id = $"0000{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 8:
                    id = $"{id}-0000-1000-8000-00805f9b34fb";
                    return Guid.Parse(id);

                case 16:
                case 32:
                    return Guid.Parse(id);

                default:
                    Log.Warn("Device", "Invalid UUID Detected - " + id);
                    return Guid.Empty;
            }
        }


        public static Guid ToGuid(this UUID uuid) =>
            Guid.ParseExact(uuid.ToString(), "d");


        public static ParcelUuid ToParcelUuid(this Guid guid) =>
            ParcelUuid.FromString(guid.ToString());


        public static UUID ToUuid(this Guid guid)
            => UUID.FromString(guid.ToString());


        public static GattPermission ToNative(this GattPermissions permissions)
            => (GattPermission)Enum.Parse(typeof(GattPermission), permissions.ToString());


        public static DroidGattStatus ToNative(this GattStatus status)
            => (DroidGattStatus)Enum.Parse(typeof(DroidGattStatus), status.ToString());


        public static GattProperty ToNative(this CharacteristicProperties properties)
        {
            if (properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired))
                throw new ArgumentException("NotifyEncryptionRequired not supported on Android");

            if (properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired))
                throw new ArgumentException("IndicateEncryptionRequired not supported on Android");

            var value = properties
                .ToString()
                .Replace(
                    CharacteristicProperties.WriteNoResponse.ToString(),
                    GattProperty.WriteNoResponse.ToString()
                )
                .Replace(
                    CharacteristicProperties.AuthenticatedSignedWrites.ToString(),
                    GattProperty.SignedWrite.ToString()
                )
                .Replace(
                    CharacteristicProperties.ExtendedProperties.ToString(),
                    GattProperty.ExtendedProps.ToString()
                );

            return (GattProperty)Enum.Parse(typeof(GattProperty), value);
        }
    }
}