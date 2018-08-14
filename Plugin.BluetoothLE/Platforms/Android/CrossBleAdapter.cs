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

            AndroidConfiguration.IsMainThreadSuggested = Build.VERSION.SdkInt < BuildVersionCodes.Kitkat || (
                Build.VERSION.SdkInt < BuildVersionCodes.N &&
                Build.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase)
            );

            AndroidConfiguration.IsServerSupported = Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;
            AndroidConfiguration.UseNewScanner =  Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;
            AndroidConfiguration.ShouldInvokeOnMainThread = true;
        }
    }
}
