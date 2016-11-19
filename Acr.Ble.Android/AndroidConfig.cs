using System;
using Android.OS;
using B = Android.OS.Build;


namespace Acr.Ble
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


        public static bool WriteOnMainThread { get; set; } = true;
    }
}
