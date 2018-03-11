using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractDevice : IDevice
    {
        protected AbstractDevice() {}
        protected AbstractDevice(string initialName, Guid uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
        }


        public virtual string Name { get; protected set; }
        public virtual  Guid Uuid { get; protected set; }
        public abstract ConnectionStatus Status { get; }
        public abstract DeviceFeatures Features { get; }
        public abstract object NativeDevice { get; }

        public abstract void Connect(GattConnectionConfig config);
        public abstract void CancelConnection();
        public abstract IConnectableObservable<int> WhenRssiUpdated(TimeSpan? timeSpan);
        public abstract IConnectableObservable<ConnectionStatus> WhenStatusChanged();
        public abstract IConnectableObservable<IGattService> WhenServiceDiscovered();

        public virtual IObservable<string> WhenNameUpdated() => throw new NotImplementedException("WhenNameUpdated is not supported on this platform");
        public virtual IObservable<IGattService> GetKnownService(Guid serviceUuid) => throw new NotImplementedException("GetKnownService is not supported on this platform");

        public virtual PairingStatus PairingStatus => PairingStatus.Unavailiable;
		public virtual IObservable<bool> PairingRequest(string pin) => throw new ArgumentException("Pairing request is not supported on this platform");

        public virtual int GetCurrentMtuSize() => 20;
        public virtual IObservable<int> RequestMtu(int size) => Observable.Return(this.GetCurrentMtuSize());
        public virtual IConnectableObservable<int> WhenMtuChanged() => Observable.Empty<int>().Publish();
        public virtual IGattReliableWriteTransaction BeginReliableWriteTransaction() => new VoidGattReliableWriteTransaction();
    }
}
