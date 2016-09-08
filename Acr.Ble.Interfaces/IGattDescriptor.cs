using System;


namespace Acr.Ble
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }

        Guid Uuid { get; }
        string Description { get; }
        byte[] Value { get; }
        IObservable<object> Write(byte[] data);
        IObservable<byte[]> Read();
    }
}
