using System;


namespace Acr.Ble
{
    public interface IGattReliableWriteTransaction : IDisposable
    {
        IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value);
        IObservable<object> Commit();
        void Abort();
    }
}