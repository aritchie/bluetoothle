using System;
using System.Collections.Generic;


namespace Acr.Ble
{
    public interface IGattService
    {
        IDevice Device { get; }

        Guid Uuid { get; }
        string Description { get; }
        IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
        IReadOnlyCollection<IGattCharacteristic> DiscoveredCharacteristics { get; }
    }
}
