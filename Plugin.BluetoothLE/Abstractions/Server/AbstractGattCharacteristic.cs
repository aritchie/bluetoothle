using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Plugin.BluetoothLE.Server
{
    public abstract class AbstractGattCharacteristic : IGattCharacteristic
    {
        protected AbstractGattCharacteristic(IGattService service,
                                             Guid characteristicUuid,
                                             CharacteristicProperties properties,
                                             GattPermissions permissions)
        {
            this.Service = service;
            this.Uuid = characteristicUuid;
            this.Properties = properties;
            this.Permissions = permissions;

            this.InternalDescriptors = new List<IGattDescriptor>();
            this.Descriptors = new ReadOnlyCollection<IGattDescriptor>(this.InternalDescriptors);
        }


        public IGattDescriptor AddDescriptor(Guid uuid, byte[] value)
        {
            var native = this.CreateNative(uuid, value);
            this.InternalDescriptors.Add(native);
            return native;
        }


        protected IList<IGattDescriptor> InternalDescriptors { get; }

        public IGattService Service { get; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public GattPermissions Permissions { get; }
        public IReadOnlyList<IGattDescriptor> Descriptors { get; }
        public abstract IReadOnlyList<IDevice> SubscribedDevices { get; }
        public abstract void Broadcast(byte[] value, params IDevice[] devices);
        public abstract IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IDevice[] devices);
        public abstract IObservable<WriteRequest> WhenWriteReceived();
        public abstract IObservable<ReadRequest> WhenReadReceived();
        public abstract IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged();

        protected abstract IGattDescriptor CreateNative(Guid uuid, byte[] value);
    }
}
