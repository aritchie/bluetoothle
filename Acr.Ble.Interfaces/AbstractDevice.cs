using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;


namespace Acr.Ble
{
    public abstract class AbstractDevice : IDevice, IDisposable
    {
        protected IDictionary<Guid, IGattService> Services { get; }


        protected AbstractDevice(string initialName, Guid uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
            this.Services = new Dictionary<Guid, IGattService>();
        } 


        ~AbstractDevice()
        {
            this.Dispose(false);
        }


        public string Name { get; protected set; }
        public Guid Uuid { get; protected set; }
        public abstract ConnectionStatus Status { get; }

        public abstract void CancelConnection();
        public abstract IObservable<object> Connect();
        public abstract IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan);
        public abstract IObservable<ConnectionStatus> WhenStatusChanged();
        public abstract IObservable<IGattService> WhenServiceDiscovered();
        public abstract IObservable<string> WhenNameUpdated();


        public virtual IObservable<IGattService> FindServices(params Guid[] serviceUuids)
        {
            return this.WhenServiceDiscovered()
                       .Take(1)
                       .Where(x => serviceUuids.Any(y => y.Equals(x)));
        }


        public virtual PairingStatus PairingStatus => PairingStatus.Unavailiable;
        public virtual bool IsPairingRequestSupported => false;
        public virtual IObservable<bool> PairingRequest(string pin)
        {
            throw new ArgumentException("Pairing request is not supported on this platform");
        }


        public virtual bool IsMtuRequestAvailable => false;
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


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
        }
    }
}
