using System;
using System.Reactive.Linq;
using System.Threading.Tasks;


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

        public abstract IObservable<object> Connect(GattConnectionConfig config);
        public abstract IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan);
        public abstract IObservable<ConnectionStatus> WhenStatusChanged();
        public abstract IObservable<IGattService> WhenServiceDiscovered();

        public virtual IObservable<string> WhenNameUpdated() => throw new NotImplementedException("WhenNameUpdated is not supported on this platform");
        public virtual IObservable<IGattService> GetKnownService(Guid serviceUuid) => throw new NotImplementedException("GetKnownService is not supported on this platform");


        public virtual void CancelConnection()
        {
            this.cancelReconnect = true;
            this.ReconnectSubscription?.Dispose();
            this.ReconnectSubscription = null;
        }


        public virtual PairingStatus PairingStatus => PairingStatus.Unavailiable;
		public virtual IObservable<bool> PairingRequest(string pin) => throw new ArgumentException("Pairing request is not supported on this platform");


        public virtual int GetCurrentMtuSize() => 20;
        public virtual IObservable<int> RequestMtu(int size) => Observable.Return(this.GetCurrentMtuSize());
        public virtual IObservable<int> WhenMtuChanged() => Observable.Empty<int>();
        public virtual IGattReliableWriteTransaction BeginReliableWriteTransaction() => new VoidGattReliableWriteTransaction();


        bool cancelReconnect = false;
        protected IDisposable ReconnectSubscription { get; set; }
        protected virtual void SetupAutoReconnect(GattConnectionConfig config)
        {
            if (this.ReconnectSubscription != null)
                return;

            if (!config.IsPersistent)
                return;

            this.cancelReconnect = false;
            this.ReconnectSubscription = this.WhenStatusChanged()
                .Skip(1) // skip the initial registration
                .Where(x => x == ConnectionStatus.Disconnected)
                .Subscribe(async x =>
                {
                    while (!this.cancelReconnect && this.Status != ConnectionStatus.Connected)
                    {
                        try
                        {
                            await Task.Delay(300);
                            if (!this.cancelReconnect)
                                await this.Connect(config);
                        }
                        catch (Exception ex)
                        {
                            Log.Out("Failed to reconnect to device - " + ex);
                        }
                    }
                });
        }
    }
}
