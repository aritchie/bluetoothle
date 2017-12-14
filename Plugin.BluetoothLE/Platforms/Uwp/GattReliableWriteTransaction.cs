using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction;


namespace Plugin.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly Native native;


        public GattReliableWriteTransaction()
        {
            this.native = new Native();
        }


        public override IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            this.AssertAction();

            var platform = characteristic as GattCharacteristic;
            if (platform == null)
                throw new ArgumentException("");

            // TODO: need write observable
            this.native.WriteValue(platform.Native, null);
            return null;
        }


        public override IObservable<object> Commit()
        {
            this.AssertAction();

            return Observable.Create<object>(async ob =>
            {
                this.Status = TransactionStatus.Committing;

                var result = await this.native.CommitAsync();
                if (result == GattCommunicationStatus.Success)
                {
                    this.Status = TransactionStatus.Committed;
                    ob.Respond(null);
                }
                else
                {
                    this.Status = TransactionStatus.Aborted;
                    ob.OnError(new GattReliableWriteTransactionException("Failed to write transaction"));
                }
                return Disposable.Empty;
            });
        }


        public override void Abort()
        {
            this.AssertAction();
            // TODO: how to abort?
        }
    }
}
