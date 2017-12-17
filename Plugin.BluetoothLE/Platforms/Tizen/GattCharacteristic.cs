using System;
using System.Reactive;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        public GattCharacteristic(IGattService service, Guid uuid, CharacteristicProperties properties) : base(service, uuid, properties)
        {
        }


        public override IObservable<Unit> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            throw new NotImplementedException();
        }


        public override IObservable<Unit> DisableNotifications()
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> WhenNotificationReceived()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            throw new NotImplementedException();
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}
