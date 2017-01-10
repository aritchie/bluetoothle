using System;


namespace Acr.Ble
{
    public interface IGattService
    {
        IDevice Device { get; }

        Guid Uuid { get; }
        string Description { get; }
        IObservable<IGattCharacteristic> FindCharacteristics(params Guid[] characteristicUuids);
        IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
    }
}
