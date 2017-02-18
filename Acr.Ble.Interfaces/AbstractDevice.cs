using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractDevice : IDevice, IDisposable
    {
        protected AbstractDevice(string initialName, Guid uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
        }


        ~AbstractDevice()
        {
            this.Dispose(false);
        }


        public string Name { get; protected set; }
        public Guid Uuid { get; protected set; }
        public abstract ConnectionStatus Status { get; }
        public abstract DeviceFeatures Features { get; }

        public abstract IObservable<object> Connect(GattConnectionConfig config);
        public abstract IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan);
        public abstract IObservable<ConnectionStatus> WhenStatusChanged();
        public abstract IObservable<IGattService> WhenServiceDiscovered();
        public abstract IObservable<string> WhenNameUpdated();


        public virtual void CancelConnection()
        {
            this.cancelReconnect = true;
            this.ReconnectSubscription?.Dispose();
            this.ReconnectSubscription = null;
        }


        public virtual PairingStatus PairingStatus => PairingStatus.Unavailiable;
        public virtual IObservable<bool> PairingRequest(string pin)
        {
            throw new ArgumentException("Pairing request is not supported on this platform");
        }


        public virtual int GetCurrentMtuSize()
        {
            return 20;
        }


        public virtual IObservable<int> RequestMtu(int size)
        {
            return Observable.Return(this.GetCurrentMtuSize());
        }


        public virtual IObservable<int> WhenMtuChanged()
        {
            return Observable.Empty<int>();
        }


        public virtual IGattReliableWriteTransaction BeginReliableWriteTransaction()
        {
            return new VoidGattReliableWriteTransaction();
        }


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        public virtual void Dispose(bool disposing)
        {
            this.ReconnectSubscription?.Dispose();
            this.ReconnectSubscription = null;
        }


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
                            await this.Connect(config);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Failed to reconnect to device - " + ex);
                        }
                    }
                });
        }
    }
}
