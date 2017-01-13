using System;
using System.Reactive.Linq;
using Acr.Ble.Internals;
using Java.Lang;


namespace Acr.Ble
{
    public class GattReliableWriteTransaction : IGattReliableWriteTransaction
    {
        readonly GattContext context;
        bool committed;


        public GattReliableWriteTransaction(GattContext context)
        {
            this.context = context;
            this.context.Gatt.BeginReliableWrite();
        }


        ~GattReliableWriteTransaction()
        {
            this.Dispose(false);
        }


        public IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            // just write to the standard characteristic write
            return characteristic.Write(value);
        }


        public IObservable<object> Commit()
        {
            return Observable.Create<object>(ob =>
            {
                var handler = new EventHandler<GattEventArgs>((sender, args) =>
                {
                    if (args.IsSuccessful)
                        ob.Respond(null);
                    else
                        ob.OnError(new GattReliableWriteTransactionException("Error committing transaction"));
                });

                this.context.Callbacks.ReliableWriteCompleted += handler;
                this.context.Gatt.ExecuteReliableWrite();
                this.committed = true;

                return () => this.context.Callbacks.ReliableWriteCompleted -= handler;
            });
        }


        public void Abort()
        {
            this.context.Gatt.AbortReliableWrite();
        }


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!this.committed)
                this.Abort();
        }
    }
}