using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Plugin.BluetoothLE.Server
{
    public abstract class AbstractGattService : IGattService
    {
        protected AbstractGattService(IGattServer server, Guid serviceUuid, bool primary)
        {
            this.Server = server;

            this.Uuid = serviceUuid;
            this.IsPrimary = primary;

            this.internalList = new List<IGattCharacteristic>();
            this.Characteristics = new ReadOnlyCollection<IGattCharacteristic>(this.internalList);
        }


        public IGattServer Server { get; }
        public Guid Uuid { get; }
        public bool IsPrimary { get; }


        public virtual IGattCharacteristic AddCharacteristic(Guid uuid, CharacteristicProperties properties, GattPermissions permissions)
        {
            var characteristic = this.CreateNative(uuid, properties, permissions);
            this.internalList.Add(characteristic);
            return characteristic;
        }


        readonly IList<IGattCharacteristic> internalList;
        public IReadOnlyList<IGattCharacteristic> Characteristics { get; }
        protected abstract IGattCharacteristic CreateNative(Guid uuid, CharacteristicProperties properties, GattPermissions permissions);
    }
}
