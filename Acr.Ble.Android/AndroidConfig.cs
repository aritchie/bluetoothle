using System;
using Android.App;
using Android.OS;
using B = Android.OS.Build;


namespace Plugin.BluetoothLE
{
    public static class AndroidConfig
    {
        /// <summary>
        /// Forces library to use Pre-Lollipop BLE mechanisms
        /// </summary>
        public static bool ForcePreLollipopScanner { get; set; }

        /// <summary>
        /// Thread used to connect
        /// </summary>
        public static ConnectionThread ConnectionThread { get; set; }


        /// <summary>
        /// Returns the suggested connection thread to use.  It does not set and perform any logic against the library.
        /// This will update periodically overtime, but it is suggested you do your own due diligence with the devices you work with
        /// </summary>
        public static ConnectionThread SuggestedConnectionThread
        {
            get
            {
                if (B.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                    return ConnectionThread.Default;

                if (!B.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase))
                    return ConnectionThread.MainThread;

                return ConnectionThread.Default;
            }
        }


        /// <summary>
        /// This is suggested for most Android devices to be true (defaults to true)
        /// </summary>
        public static bool WriteOnMainThread { get; set; } = true;


        public static void SyncPost(this Action action)
        {
            if (AndroidConfig.WriteOnMainThread)
            {
                Application.SynchronizationContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
    }
}
