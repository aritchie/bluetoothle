using System;
using System.Text;
using Foundation;
using CoreBluetooth;


namespace Plugin.BluetoothLE
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
            return CBUUID.FromString(guid.ToString());
        }


        public static NSUuid ToNSUuid(this Guid guid)
        {
            return new NSUuid(guid.ToString());
        }
    }
}