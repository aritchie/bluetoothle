using System;


namespace Plugin.BluetoothLE
{
    public interface IGattReliableWriteTransaction : IDisposable
    {
        TransactionStatus Status { get; }
        IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value);
        IObservable<object> Commit();
        void Abort();
    }
}