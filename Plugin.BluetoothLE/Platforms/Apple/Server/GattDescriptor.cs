using System;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE.Server
{
    public class GattDescriptor : AbstractGattDescriptor, IAppleGattDescriptor
    {
        public CBMutableDescriptor Native { get; }


        public GattDescriptor(IGattCharacteristic characteristic,
                              Guid descriptorUuid,
                              byte[] value) : base(characteristic, descriptorUuid, value)
        {
            this.Native = new CBMutableDescriptor(descriptorUuid.ToCBUuid(), NSData.FromArray(value));
        }
    }
}
