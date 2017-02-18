using System;
using Android.OS;
using Java.Util;


namespace Plugin.BluetoothLE
{
    internal static class Extensions
    {
        public static Guid ToGuid(this byte[] uuidBytes)
        {
            if (uuidBytes.Length == 16)
                return new Guid(uuidBytes);

            var id = BitConverter.ToString(uuidBytes).Replace("-", String.Empty);
            switch (id.Length)
            {
                case 4:
                    id = $"0000{id}-0000-1000-8000-00805f9b34fb";
                    break;

                case 8:
                    id = $"{id}-0000-1000-8000-00805f9b34fb";
                    break;
            }
            return Guid.ParseExact(id, "d");
        }


        public static Guid ToGuid(this UUID uuid)
        {
            return Guid.ParseExact(uuid.ToString(), "d");
        }


        // TODO: is this working?
        public static ParcelUuid ToParcelUuid(this Guid guid)
        {
            return ParcelUuid.FromString(guid.ToString());
        }


        public static UUID ToUuid(this Guid guid)
        {
            return UUID.FromString(guid.ToString());
        }
    }
}