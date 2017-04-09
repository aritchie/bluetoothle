using System;
using System.Collections.Generic;
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
            var native = ConvertFlags<CBCharacteristicProperties>(properties);
            return native;
        }


        static T ConvertFlags<T>(Enum flags1)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(typeof(T) + " is not an enum!");

            var values = new List<string>();
            var allValues = Enum.GetValues(flags1.GetType());
            foreach (var all in allValues)
            {
                if (flags1.HasFlag((Enum)all))
                    values.Add(all.ToString());
            }
            var raw = String.Join(",", values.ToArray());
            var result = (T)Enum.Parse(typeof(T), raw);

            return result;
        }
    }
}