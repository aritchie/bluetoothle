using System;
using System.Reactive.Linq;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly GattContext context;


        public GattReliableWriteTransaction(GattContext context)
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


        public override IObservable<object> Commit()
        {
            this.AssertAction();

            return Observable.Create<object>(ob =>
            {
                var handler = new EventHandler<GattEventArgs>((sender, args) =>
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

                this.context.Callbacks.ReliableWriteCompleted += handler;
                this.context.Gatt.ExecuteReliableWrite();
                this.Status = TransactionStatus.Committing;

                return () => this.context.Callbacks.ReliableWriteCompleted -= handler;
            });
        }


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