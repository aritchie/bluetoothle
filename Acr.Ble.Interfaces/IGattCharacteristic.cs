using System;


namespace Acr.Ble
{
    public interface IGattCharacteristic
    {
        IGattService Service { get; }
        Guid Uuid { get; }
        string Description { get; }
        bool IsNotifying { get; }
        CharacteristicProperties Properties { get; }
        byte[] Value { get; }

        IObservable<byte[]> WhenNotificationOccurs();
        IObservable<IGattDescriptor> WhenDescriptorDiscovered();
        IObservable<object> Write(byte[] value);
        IObservable<byte[]> Read();
    }
}
