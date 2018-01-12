using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE.Server
{
    public abstract class AbstractGattServer : IGattServer
    {

        protected AbstractGattServer()
        {
            this.ServiceList = new List<IGattService>();
            this.Services = new ReadOnlyCollection<IGattService>(this.ServiceList);
        }


        ~AbstractGattServer() => this.Dispose(false);


        protected IList<IGattService> ServiceList { get; }
        public IReadOnlyList<IGattService> Services { get; }


        IObservable<CharacteristicSubscription> chOb;
        public virtual IObservable<CharacteristicSubscription> WhenAnyCharacteristicSubscriptionChanged()
        {
            this.chOb = this.chOb ?? Observable.Create<CharacteristicSubscription>(ob =>
            {
                var cleanup = this.Services
                    .SelectMany(x => x.Characteristics)
                    .Select(x => x.WhenDeviceSubscriptionChanged().Subscribe(y =>
                        ob.OnNext(new CharacteristicSubscription(x, y.Device, y.IsSubscribed))
                    ))
                    .ToList();

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


        public virtual void AddService(IGattService service)
        {
            this.ServiceList.Add(service);
            this.AddNative(service);
        }


        public virtual void AddService(Guid uuid, bool primary, Action<IGattService> callback)
        {
            var service = this.CreateService(uuid, primary);
            callback(service);
            this.AddService(service);
        }


        public void RemoveService(Guid serviceUuid)
        {
            var service = this.Services.FirstOrDefault(x => x.Uuid.Equals(serviceUuid));
            if (service != null)
            {
                this.RemoveNative(service);
                this.ServiceList.Remove(service);
            }
        }


        public void ClearServices()
        {
            this.ClearNative();
            this.ServiceList.Clear();
        }


        public abstract IGattService CreateService(Guid uuid, bool primary);
        protected abstract void AddNative(IGattService service);
        protected abstract void RemoveNative(IGattService service);
        protected abstract void ClearNative();


        public void Dispose() => this.Dispose(true);
        protected virtual void Dispose(bool disposing) { }
    }
}
