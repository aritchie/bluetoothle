using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE.Server
{
    public interface IGattCharacteristic
    {
        IGattService Service { get; }

        // permissions
        Guid Uuid { get; }
        CharacteristicProperties Properties { get; }
        GattPermissions Permissions { get; }

        IGattDescriptor AddDescriptor(Guid uuid, byte[] value);
        IReadOnlyList<IGattDescriptor> Descriptors { get; }

        /// <summary>
        /// Send null to broadcast to all
        /// Subscription can be used to tell when one or all devices have been broadcast to
        /// This is considered a HOT observable - it will run even if you don't listen
        /// </summary>
        /// <param name="value"></param>
        /// <param name="devices">Don't pass any to broadcast to all devices, otherwise, pass your selected devices</param>
        IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IDevice[] devices);

        void Broadcast(byte[] value, params IDevice[] device);

        IObservable<WriteRequest> WhenWriteReceived();
        IObservable<ReadRequest> WhenReadReceived();
        IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged();
        IReadOnlyList<IDevice> SubscribedDevices { get; }
    }
}
