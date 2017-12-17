using System;
using System.Reactive;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public class VoidGattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        public override IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
            => characteristic.Write(value);


        public override IObservable<Unit> Commit()
        {
            this.AssertAction();
            this.Status = TransactionStatus.Committed;

            return Observable.Return(Unit.Default);
        }


        public override void Abort()
        {
            this.AssertAction();
            this.Status = TransactionStatus.Aborted;
        }
    }
}
