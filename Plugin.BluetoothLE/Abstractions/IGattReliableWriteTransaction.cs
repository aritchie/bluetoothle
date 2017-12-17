using System;
using System.Reactive;


namespace Plugin.BluetoothLE
{
    public interface IGattReliableWriteTransaction : IDisposable
    {
        TransactionStatus Status { get; }
        IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value);
        IObservable<Unit> Commit();
        void Abort();
    }
}