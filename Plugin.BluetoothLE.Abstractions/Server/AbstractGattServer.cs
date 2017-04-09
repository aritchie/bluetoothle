using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Plugin.BluetoothLE.Server
{
    public abstract class AbstractGattServer : IGattServer
    {
        readonly IList<IGattService> internalList;


        protected AbstractGattServer()
        {
            this.internalList = new List<IGattService>();
            this.Services = new ReadOnlyCollection<IGattService>(this.internalList);
        }


        ~AbstractGattServer()
        {
            this.Dispose(false);
        }


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            this.Stop();
        }


        public IReadOnlyList<IGattService> Services { get; }

        public abstract IObservable<bool> WhenRunningChanged();
        public abstract bool IsRunning { get; }
        public abstract Task Start(AdvertisementData adData);
        public abstract void Stop();


        IObservable<CharacteristicSubscription> chOb;
        public virtual IObservable<CharacteristicSubscription> WhenAnyCharacteristicSubscriptionChanged()
        {
            this.chOb = this.chOb ?? Observable.Create<CharacteristicSubscription>(ob =>
            {
                var cleanup = new List<IDisposable>();
                foreach (var s in this.Services)
                {
                    foreach (var ch in s.Characteristics)
                    {
                        cleanup.Add(ch.WhenDeviceSubscriptionChanged().Subscribe(x =>
                        {
                            ob.OnNext(new CharacteristicSubscription(ch, x.Device, x.IsSubscribed));
                        }));
                    }
                }
                return () =>
                {
                    foreach (var dispose in cleanup)
                        dispose.Dispose();
                };
            })
            .Publish()
            .RefCount();

            return this.chOb;
        }


        public virtual IList<IDevice> GetAllSubscribedDevices()
        {
            var list = new Dictionary<Guid, IDevice>();
            foreach (var s in this.Services)
            {
                foreach (var ch in s.Characteristics)
                {
                    foreach (var d in ch.SubscribedDevices)
                    {
                        if (!list.ContainsKey(d.Uuid))
                            list.Add(d.Uuid, d);
                    }
                }
            }
            return list.Values.ToList();
        }


        public IGattService AddService(Guid uuid, bool primary)
        {
            var native = this.CreateNative(uuid, primary);
            this.internalList.Add(native);
            return native;
        }


        public void RemoveService(Guid serviceUuid)
        {
            var service = this.Services.FirstOrDefault(x => x.Uuid.Equals(serviceUuid));
            if (service != null)
            {
                this.RemoveNative(service);
                this.internalList.Remove(service);
            }
        }


        public void ClearServices()
        {
            this.ClearNative();
            this.internalList.Clear();
        }


        protected abstract IGattService CreateNative(Guid uuid, bool primary);
        protected abstract void ClearNative();
        protected abstract void RemoveNative(IGattService service);
    }
}
