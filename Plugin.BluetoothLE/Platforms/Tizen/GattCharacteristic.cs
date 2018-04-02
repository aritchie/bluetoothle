using System;
using System.Reactive;
using System.Reactive.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly BluetoothGattCharacteristic native;


        public GattCharacteristic(BluetoothGattCharacteristic native, IGattService service, Guid uuid, CharacteristicProperties properties) : base(service, uuid, properties)
        {
            this.native = native;
        }


        public override byte[] Value { get; }


        public override IObservable<CharacteristicGattResult> WhenNotificationReceived() =>
            Observable.Create<CharacteristicGattResult>(ob =>
            {
                this.native.ValueChanged += null;

                return () => this.native.ValueChanged -= null;
            });

        public override IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicGattResult> DisableNotifications()
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicGattResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}
