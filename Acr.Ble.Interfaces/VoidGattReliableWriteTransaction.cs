using System;
using System.Reactive.Linq;


namespace Acr.Ble
{
    public class VoidGattReliableWriteTransaction : AbstractGattReliableWriteTransaction
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
            if (this.Status == TransactionStatus.Active)
                this.Status = TransactionStatus.Committed;

            return Observable.Return(new object());
        }


        public void Abort()
        {
            if (this.Status == TransactionStatus.Active)
                this.Status = TransactionStatus.Aborted;
        }

        public TransactionStatus Status { get; private set; } = TransactionStatus.Active;
    }
}
