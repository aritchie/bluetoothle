using System;
using Android.OS;


namespace Plugin.BluetoothLE
{
    public static class AndroidBleConfiguration
    {
        public static bool IsMainThreadSuggested =>
            Build.VERSION.SdkInt < BuildVersionCodes.Kitkat || (
                Build.VERSION.SdkInt < BuildVersionCodes.N &&
                Build.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase)
            );


        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static bool ShouldInvokeOnMainThread { get; set; } = IsMainThreadSuggested;


        /// <summary>
        /// Specifies the number of Gatt.Connect attempts that will be run before handing off to NativeDevice.ConnectGatt(autoReconnect);
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static uint MaxAutoReconnectAttempts { get; set; } = 5;


        /// <summary>
        /// Time span to pause before service discovery (helps in combating GATT133 error) when service discovery is performed immediately after connection
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static TimeSpan PauseBeforeServiceDiscovery { get; set; } = TimeSpan.FromMilliseconds(750);


        /// <summary>
        /// Specifies the wait time before attempting an auto-reconnect
        /// DO NOT CHANGE if you don't know what this is!
        /// </summary>
        public static TimeSpan PauseBetweenAutoReconnectAttempts { get; set; } = TimeSpan.FromSeconds(1);


        public static bool UseNewScanner { get; set; } = Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;
    }
}
