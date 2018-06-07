﻿using System;
using System.Reactive.Linq;

namespace Plugin.BluetoothLE.Tests.Mocks
{
    public class MockGattCharacteristic : AbstractGattCharacteristic
    {
        public MockGattCharacteristic(IGattService service, Guid guid, CharacteristicProperties properties)
            : base(service, guid, properties) 
        { 
        }

        private byte[] _value;
        public override byte[] Value => this._value;

        public override IObservable<CharacteristicGattResult> DisableNotifications()
        {
            throw new NotImplementedException();
        }

        public override IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            throw new NotImplementedException();
        }

        public override IObservable<CharacteristicGattResult> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            throw new NotImplementedException();
        }

        public override IObservable<CharacteristicGattResult> Read()
        {
            throw new NotImplementedException();
        }

        public override IObservable<CharacteristicGattResult> WhenNotificationReceived()
        {
            throw new NotImplementedException();
        }

        public override IObservable<CharacteristicGattResult> Write(byte[] value)
        {
            this._value = value;
            return Observable.Create<CharacteristicGattResult>(obs =>
            {
                obs.OnNext(new CharacteristicGattResult(this, value));
                obs.OnCompleted();
                return () => { };
            });
        }

        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
