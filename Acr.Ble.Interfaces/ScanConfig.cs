using System;


namespace Acr.Ble
{
    public class ScanConfig
    {
        /// <summary>
        /// Android Only - Initiates a low powered cycle scan
        /// </summary>
        public bool IsLowPoweredScan { get; set; }


        /// <summary>
        /// Filters scan to devices that advertise specified service UUID
        /// iOS - you must set this to initiate a background scan
        /// </summary>
        public Guid? ServiceUuid { get; set; }
    }
}
