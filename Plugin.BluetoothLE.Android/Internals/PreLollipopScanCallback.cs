using System;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;
using Java.Util;

namespace Plugin.BluetoothLE.Internals
{
    public class PreLollipopScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
    {
        readonly Action<ScanEventArgs> callback;


        public List<Guid> ServiceUuids { get; set; }

        public PreLollipopScanCallback(Action<ScanEventArgs> callback)
        {
            this.callback = callback;
        }


        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            if (ServiceUuids != null && ServiceUuids.Count() > 0)
            {
                var uuids = ParseUUIDs(scanRecord).Select(o => o.ToUpper());
                if (uuids.Count() == 0)
                    return;
                if (ServiceUuids.Select(o => o.ToString().ToUpper()).Intersect(uuids).Count() == 0)
                    return;
            }

            this.callback(new ScanEventArgs(device, rssi, scanRecord));
        }

        // Solution from https://stackoverflow.com/questions/18019161/startlescan-with-128-bit-uuids-doesnt-work-on-native-android-ble-implementation
        List<string> ParseUUIDs(byte[] advertisedData)
        {
            List<string> uuids = new List<string>();
            int offset = 0;
            while (offset < (advertisedData.Length - 2))
            {
                int len = advertisedData[offset++];
                if (len == 0)
                    break;

                int type = advertisedData[offset++];
                switch (type)
                {
                    case 0x02: // Partial list of 16-bit UUIDs
                    case 0x03: // Complete list of 16-bit UUIDs
                        while (len > 1)
                        {
                            int uuid16 = advertisedData[offset++];
                            uuid16 += (advertisedData[offset++] << 8);
                            len -= 2;
                            uuids.Add($"0000{uuid16.ToString("X")}-0000-1000-8000-00805f9b34fb");
                        }
                        break;
                    case 0x06:// Partial list of 128-bit UUIDs
                    case 0x07:// Complete list of 128-bit UUIDs
                              // Loop through the advertised 128-bit UUID's.
                        while (len >= 16)
                        {
                            try
                            {
                                // Wrap the advertised bits and order them.
                                Java.Nio.ByteBuffer buffer = Java.Nio.ByteBuffer.Wrap(advertisedData,
                                                                                      offset++, 16).Order(Java.Nio.ByteOrder.LittleEndian);
                                long mostSignificantBit = buffer.Long;
                                long leastSignificantBit = buffer.Long;
                                uuids.Add(new UUID(leastSignificantBit,
                                        mostSignificantBit).ToString());
                            }
                            catch (Java.Lang.IndexOutOfBoundsException e)
                            {
                                continue;
                            }
                            finally
                            {
                                // Move the offset to read the next uuid.
                                offset += 15;
                                len -= 16;
                            }
                        }
                        break;
                    default:
                        offset += (len - 1);
                        break;
                }
            }

            return uuids;
        }
    }
}