using System;
using Android.OS;
using B = Android.OS.Build;


namespace Plugin.BluetoothLE
{
    public static class AndroidConfig
    {
        public static bool MainThreadSuggested =>
            B.VERSION.SdkInt < BuildVersionCodes.Kitkat ||
            B.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase);


        public static bool PerformActionsOnMainThread { get; set; } = MainThreadSuggested;
    }
}