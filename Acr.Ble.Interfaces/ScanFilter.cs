using System;


namespace Acr.Ble
{
    public class ScanFilter
    {
        public static ScanFilter ForService(Guid serviceUuid)
        {
            return new ScanFilter
            {
                ServiceUuid = serviceUuid
            };
        }


        public static ScanFilter ForDevices(params Guid[] deviceUuids)
        {
            return new ScanFilter
            {
                DeviceUuids = deviceUuids
            };
        }


        public Guid[] DeviceUuids { get; private set; }
        public Guid ServiceUuid { get; private set; }

        public ScanMode Mode { get; set; }
    }
}