using System;
using System.Reactive.Linq;


namespace Acr.Ble
{
    public class VoidGattReliableWriteTransaction : IGattReliableWriteTransaction
    {
        public void Dispose()
        {
        }


        public IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            return characteristic.Write(value);
        }


        public IObservable<object> Commit()
        {
            return Observable.Return(new object());
        }


        public void Abort()
        {
        }
    }
}
