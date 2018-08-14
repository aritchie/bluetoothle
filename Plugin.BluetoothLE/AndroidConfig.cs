using System;


namespace Plugin.BluetoothLE
{
    public class AndroidConfig
    {
        /// <summary>
        /// This is only necessary on niche cases and thus must be enabled by default
        /// </summary>
        public bool RefreshServices { get; set; }

        /// <summary>
        /// Suggests whether main thread is to be used
        /// </summary>
        public bool IsMainThreadSuggested { get; internal set; }

        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public bool ShouldInvokeOnMainThread { get; set; } = true;


        /// <summary>
        /// This performs pauses between each operation helping android recover from itself
        /// </summary>
        public TimeSpan PauseBetweenInvocations { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Time span to pause before service discovery (helps in combating GATT133 error) when service discovery is performed immediately after connection
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public TimeSpan PauseBeforeServiceDiscovery { get; set; } = TimeSpan.FromMilliseconds(750);


        public bool UseNewScanner { get; set; }
        public bool IsServerSupported { get; internal set; }
    }
}
