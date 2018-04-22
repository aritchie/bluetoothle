using System;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattService : AbstractGattService, IAppleGattService
    {
        readonly CBPeripheralManager manager;
        public CBMutableService Native { get; }


        public GattService(CBPeripheralManager manager,
                           IGattServer server,
                           Guid serviceUuid,
                           bool primary) : base(server, serviceUuid, primary)
        {
            this.manager = manager;
            this.Native = new CBMutableService(serviceUuid.ToCBUuid(), primary);
        }


        protected override IGattCharacteristic CreateNative(Guid uuid, CharacteristicProperties properties, GattPermissions permissions)
            => new GattCharacteristic(this.manager, this, uuid, properties, permissions);
    }
}
