using System;
using Android.OS;
using Java.Util;

namespace Acr.Ble
{
    public static class Extensions
    {

        public static Guid ToGuid(this UUID uuid)
        {
            return Guid.ParseExact(uuid.ToString(), "d");
        }


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