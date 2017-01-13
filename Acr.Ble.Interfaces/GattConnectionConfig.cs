using System;


namespace Acr.Ble
{
    public class GattConnectionConfig
    {
        public static GattConnectionConfig DefaultConfiguration { get; } = new GattConnectionConfig();

        /// <summary>
        ///
        /// </summary>
        public bool IsPersistent { get;  set; } = true;

        /// <summary>
        /// Android only
        /// </summary>
        public ConnectionPriority Priority { get; set; } = ConnectionPriority.Normal;

        /// <summary>
        /// iOS only
        /// </summary>
        public bool NotifyOnConnect { get; set; }

        /// <summary>
        /// iOS/tvOS/macOS only
        /// </summary>
        public bool NotifyOnDisconnect { get; set; }

        /// <summary>
        /// iOS only
        /// </summary>
        public bool NotifyOnNotification { get; set; }
    }
}
