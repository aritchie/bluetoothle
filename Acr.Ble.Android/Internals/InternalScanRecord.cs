using System;
using System.Linq;
using System.Collections.Generic;


namespace Acr.Ble.Internals
{
    public class InternalScanRecord
    {
        public static InternalScanRecord Parse(byte[] scanRecord)
        {
            var sr = new InternalScanRecord();
            var index = 0;

            while (index < scanRecord.Length)
            {
                var len = scanRecord[index++];
                if (len == 0)
                    break;

                var type = scanRecord[index];
                if (type == 0)
                    break;

                var data = new byte[len - 1];
                Array.Copy(scanRecord, index + 1, data, 0, len - 1);

                switch ((AdvertisementRecordType)type)
                {
                    case AdvertisementRecordType.TxPowerLevel:
                        sr.TxPower = (sbyte)data[0];
                        break;

                    case AdvertisementRecordType.ShortLocalName:
                    //case AdvertisementRecordType.CompleteLocalName:
                        sr.LocalName = BitConverter.ToString(data);
                        break;

                    case AdvertisementRecordType.ManufacturerSpecificData:
                        sr.ManufacturerData = data;
                        break;
                }
                index += len;
            }            
            return sr;
        }


        InternalScanRecord()
        {
        }


        public string LocalName { get; private set; }
        public byte[] ManufacturerData { get; private set; }
        public bool IsConnectable { get; private set; }
        public int TxPower { get; private set; } 
        public IList<Guid> ServiceUuids { get; } = new List<Guid>();
    }
}
