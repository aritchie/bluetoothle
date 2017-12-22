using System;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public static class BeaconExtensions
    {
        public static Beacon ToBeacon(this IAdvertisementData adData)
        {
            return null;
        }


        // iOS WILL NOT LIKE THIS
        //        public void Start(Beacon beacon)
        //        {
        //            var writer = new DataWriter();
        //            writer.WriteBytes(beacon.ToIBeaconPacket(10));
        //            var md = new BluetoothLEManufacturerData(76, writer.DetachBuffer());
        //            this.publiser.Advertisement.ManufacturerData.Add(md);
        //            this.publiser.Start();

        //            //var trigger = new BluetoothLEAdvertisementPublisherTrigger();
        //            //trigger.Advertisement.ManufacturerData.Add(md);
        //            this.AdvertisedBeacon = beacon;
        //        }


        //public static IObservable<Beacon> ScanForBeacons(this IAdapter adapter, Guid? uuid, ushort major, ushort minor)
        //{

        //}


        //public static void AdvertiseAsBeacon(this IAdvertiser advertiser, Beacon beacon)
        //{

        //}

        //public static bool IsIBeaconPacket(byte[] data)
        //{
        //    if (data.Length < 25)
        //        return false;

        //    // apple manufacturerID - https://www.bluetooth.com/specifications/assigned-numbers/company-Identifiers
        //    if (data[0] != 76 || data[1] != 0)
        //        return false;

        //    return true;
        //}


        //static byte[] ToBytes(Guid guid)
        //{
        //    var hex = guid
        //        .ToString()
        //        .Replace("-", String.Empty)
        //        .Replace("{", String.Empty)
        //        .Replace("}", String.Empty)
        //        .Replace(":", String.Empty)
        //        .Replace("-", String.Empty);

        //    var bytes = Enumerable.Range(0, hex.Length)
        //        .Where(x => x % 2 == 0)
        //        .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
        //        .ToArray();

        //    return bytes;
        //}


        //public byte[] ToIBeaconPacket()
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        using (var br = new BinaryWriter(ms))
        //        {
        //            br.Write(76);
        //            br.Write(new byte[] { 0, 0, 0 });
        //            br.Write(ToBytes(this.Uuid));
        //            br.Write(BitConverter.GetBytes(this.Major).Reverse().ToArray());
        //            br.Write(BitConverter.GetBytes(this.Minor).Reverse().ToArray());
        //            br.Write(0); // tx power
        //        }
        //        return ms.ToArray();
        //    }
        //}


        //public static double CalculateAccuracy(int txpower, double rssi)
        //{
        //    var ratio = rssi * 1 / txpower;
        //    if (ratio < 1.0)
        //        return Math.Pow(ratio, 10);

        //    var accuracy = 0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
        //    return accuracy;
        //}


        //public static Proximity CalculateProximity(double accuracy)
        //{
        //    if (accuracy < 0)
        //        return Proximity.Unknown;

        //    if (accuracy < 0.5)
        //        return Proximity.Immediate;

        //    if (accuracy <= 4.0)
        //        return Proximity.Near;

        //    return Proximity.Far;
        //}
    }
}