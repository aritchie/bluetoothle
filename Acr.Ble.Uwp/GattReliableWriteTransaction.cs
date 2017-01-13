using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction;


namespace Acr.Ble
{
    public class GattReliableWriteTransaction : IGattReliableWriteTransaction
    {
        readonly Native native;
        bool committed;


        public GattReliableWriteTransaction()
        {
            this.native = new Native();
        }


        public void Dispose()
        {
            if (!this.committed)
                this.Abort();
        }


        public IObservable<CharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            var platform = characteristic as GattCharacteristic;
            if (platform == null)
                throw new ArgumentException("");

            // TODO: need write observable
            this.native.WriteValue(platform.Native, null);
            this.committed = true;
            return null;
        }


        public IObservable<object> Commit()
        {
            return Observable.Create<object>(async ob =>
            {
                var result = await this.native.CommitAsync();
                if (result == GattCommunicationStatus.Success)
                    ob.Respond(null);
                else
                    ob.OnError(new GattReliableWriteTransactionException("Failed to write transaction"));

                return Disposable.Empty;
            });
        }


        public void Abort()
        {
            // TODO: how to abort?
        }
    }
}
