using System;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public class VoidGattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {


        public override IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            return characteristic.Write(value);
        }


        public override IObservable<object> Commit()
        {
            this.AssertAction();
            this.Status = TransactionStatus.Committed;

            return Observable.Return(new object());
        }


        public override void Abort()
        {
            this.AssertAction();
            this.Status = TransactionStatus.Aborted;
        }
    }
}
