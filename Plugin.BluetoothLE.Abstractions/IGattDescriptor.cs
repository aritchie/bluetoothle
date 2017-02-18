using System;


namespace Plugin.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }

        Guid Uuid { get; }
        string Description { get; }
        byte[] Value { get; }

        IObservable<DescriptorResult> Write(byte[] data);
        IObservable<DescriptorResult> WhenWritten();
        IObservable<DescriptorResult> Read();
        IObservable<DescriptorResult> WhenRead();
    }
}
