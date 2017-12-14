using System;
using Android.Bluetooth;
using Java.Util;
using Plugin.BluetoothLE.Server.Internals;


namespace Plugin.BluetoothLE.Server
{
    public class GattService : AbstractGattService, IDroidGattService
    {
        public BluetoothGattService Native { get; }
        readonly GattContext context;


        public GattService(GattContext context, IGattServer server, Guid uuid, bool primary) : base(server, uuid, primary)
        {
            this.context = context;
            this.Native = new BluetoothGattService(
                UUID.FromString(uuid.ToString()),
                primary ? GattServiceType.Primary : GattServiceType.Secondary
            );
        }


        protected override IGattCharacteristic CreateNative(Guid uuid, CharacteristicProperties properties, GattPermissions permissions)
        {
            return new GattCharacteristic(this.context, this, uuid, properties, permissions);
        }
    }
}
