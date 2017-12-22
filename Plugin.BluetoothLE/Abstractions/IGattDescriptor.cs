using System;


namespace Plugin.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }

        Guid Uuid { get; }
        string Description { get; }
        byte[] Value { get; }

        IObservable<DescriptorGattResult> Write(byte[] data);
        IObservable<DescriptorGattResult> WhenWritten();
        IObservable<DescriptorGattResult> Read();
        IObservable<DescriptorGattResult> WhenRead();
    }
}
