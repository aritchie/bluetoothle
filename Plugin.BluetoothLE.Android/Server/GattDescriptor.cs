using System;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattDescriptor : AbstractGattDescriptor, IDroidGattDescriptor
    {
        public GattDescriptor(IGattCharacteristic characteristic,
                              Guid descriptorUuid,
                              byte[] value) : base(characteristic, descriptorUuid, value)
        {
            this.Native = new BluetoothGattDescriptor(
                descriptorUuid.ToUuid(),
                GattDescriptorPermission.Read // TODO
            );
            this.Native.SetValue(value);
        }


        public BluetoothGattDescriptor Native { get; }
    }
}