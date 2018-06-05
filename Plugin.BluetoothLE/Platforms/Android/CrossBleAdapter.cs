using System;
using Android.App;
using Android.OS;

[assembly: UsesPermission("android.permission.BLUETOOTH")]
[assembly: UsesPermission("android.permission.BLUETOOTH_ADMIN")]
[assembly: UsesPermission("android.permission.ACCESS_COARSE_LOCATION")]

namespace Plugin.BluetoothLE
{
    public static partial class CrossBleAdapter
    {
        static CrossBleAdapter()
        {
            Current = new Adapter();
            AdapterScanner = new AdapterScanner();
        }


        public static bool IsMainThreadSuggested =>
            Build.VERSION.SdkInt < BuildVersionCodes.Kitkat || (
                Build.VERSION.SdkInt < BuildVersionCodes.N &&
                Build.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase)
            );


        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static bool ShouldInvokeOnMainThread { get; set; } = true;


        /// <summary>
        /// This performs pauses between each operation helping android recover from itself
        /// </summary>
        public static TimeSpan? PauseBetweenInvocations { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Time span to pause before service discovery (helps in combating GATT133 error) when service discovery is performed immediately after connection
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static TimeSpan PauseBeforeServiceDiscovery { get; set; } = TimeSpan.FromMilliseconds(750);


        public static bool UseNewScanner { get; set; } = Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;
    }
}
