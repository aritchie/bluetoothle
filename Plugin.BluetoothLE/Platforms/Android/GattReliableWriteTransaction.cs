using System;
using Plugin.BluetoothLE.Internals;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly DeviceContext context;


        public GattReliableWriteTransaction(DeviceContext context)
        {
            this.context = context;
            this.context.Gatt.BeginReliableWrite();
        }


        public override IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            // just write to the standard characteristic write
            this.AssertAction();
            return characteristic.Write(value);
        }


        public override IObservable<object> Commit() => this.context.Lock(Observable.Create<object>(ob =>
        {
            this.AssertAction();

            var sub = this.context
                .Callbacks
                .ReliableWriteCompleted
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                    {
                        this.Status = TransactionStatus.Committed;
                        ob.Respond(null);
                    }
                    else
                    {
                        this.Status = TransactionStatus.Aborted; // TODO: or errored?
                        ob.OnError(new GattReliableWriteTransactionException("Error committing transaction"));
                    }
                });
            this.context.Gatt.ExecuteReliableWrite();
            this.Status = TransactionStatus.Committing;

            return sub;
        }));


        public override void Abort()
        {
            this.AssertAction();
            this.context.Gatt.AbortReliableWrite();
            this.Status = TransactionStatus.Aborted;
        }


        protected override void Dispose(bool disposing)
        {
        }
    }
}