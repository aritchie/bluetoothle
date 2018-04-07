using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Acr;
using CoreBluetooth;
using Foundation;
using UIKit;


namespace Plugin.BluetoothLE
{
    public partial class GattCharacteristic : AbstractGattCharacteristic
    {
        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value) => Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertWrite(false);

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                var type = this.Peripheral.CanSendWriteWithoutResponse
                    ? CBCharacteristicWriteType.WithoutResponse
                    : CBCharacteristicWriteType.WithResponse;
                this.Write(ob, type, value);
            }
            else
            {
                this.Write(ob, CBCharacteristicWriteType.WithoutResponse, value);
            }
            return  Disposable.Empty;
        });


        void Write(IObserver<CharacteristicGattResult> ob, CBCharacteristicWriteType type, byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, type);
            var result = this.ToResult(GattEvent.Write, value);
            ob.Respond(result);
        }
    }
}