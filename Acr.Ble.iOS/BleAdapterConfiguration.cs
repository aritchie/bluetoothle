using System;
using CoreFoundation;


namespace Plugin.BluetoothLE
{
    public class BleAdapterConfiguration
    {
        public static BleAdapterConfiguration DefaultBackgroudingConfig { get; } = new BleAdapterConfiguration();


        /// <summary>
        /// This will display an alert dialog when the user powers off their bluetooth adapter
        /// </summary>
        public bool ShowPowerAlert { get; set; } = true;


        /// <summary>
        /// CBCentralInitOptions restoration key for background restoration
        /// </summary>
        public string RestoreIdentifier { get; set; } = "acr";


        /// <summary>
        /// The scan dispatch queue to use - don't touch this if you don't know what it does
        /// </summary>
        public DispatchQueue DispatchQueue { get; set; } = new DispatchQueue("acrble");
    }
}
