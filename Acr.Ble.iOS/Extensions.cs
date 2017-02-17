using System;
using System.Text;
using Foundation;
using CoreBluetooth;


namespace Acr.Ble
{
    internal static class BleExtensions
    {
        public static Guid ToGuid(this NSUuid uuid)
        {
            return Guid.ParseExact(uuid.AsString(), "d");
        }


        public static Guid ToGuid(this CBUUID uuid)
        {
            var id = uuid.ToString();
            if (id.Length == 4)
                id = $"0000{id}-0000-1000-8000-00805f9b34fb";

            return Guid.ParseExact(id, "d");
        }


        public static CBUUID ToCBUuid(this Guid guid)
        {
            // guids tobytes endianizes the result, thereby changing the actual guid for everything else, this is the quickest fix
            var bytes = Encoding.UTF8.GetBytes(guid.ToString());
            return CBUUID.FromBytes(bytes);
            //return CBUUID.FromBytes(guid.ToByteArray());
        }


        public static NSUuid ToNSUuid(this Guid guid)
        {
            return new NSUuid(guid.ToString());
        }
    }
}