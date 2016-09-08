using System;
using Java.Util;

namespace Acr.Ble
{
    public static class Extensions
    {

        public static Guid ToGuid(this UUID uuid) 
        {
            return Guid.ParseExact(uuid.ToString(), "d");
        }
    }
}

