using System;


namespace Plugin.BluetoothLE
{
    public interface IGattService
    {
        IDevice Device { get; }

        /// <summary>
        /// The service UUID
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        /// A general description of what the services if known
        /// </summary>
        string Description { get; }

        /// <summary>
        /// This will return a repeatable observable of discovered characteristics
        /// </summary>
        IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
    }
}
