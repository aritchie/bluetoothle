using System;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        public GattCharacteristic(IGattService service, Guid uuid, CharacteristicProperties properties) : base(service, uuid, properties)
        {
        }


        public override IObservable<bool> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            throw new NotImplementedException();
        }


        public override IObservable<object> DisableNotifications()
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
