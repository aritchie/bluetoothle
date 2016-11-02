using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;


namespace Acr.Ble
{
    public abstract class AbstractDevice : IDevice
    {
        protected IDictionary<Guid, IGattService> Services { get; }


        protected AbstractDevice(string initialName, Guid uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
            this.Services = new Dictionary<Guid, IGattService>();
        }


        public string Name { get; protected set; }
        public Guid Uuid { get; protected set; }
        public abstract ConnectionStatus Status { get; }

        public abstract void Disconnect();
        public abstract IObservable<object> Connect();
        public abstract IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan);
        public abstract IObservable<ConnectionStatus> WhenStatusChanged();
        public abstract IObservable<IGattService> WhenServiceDiscovered();
        public abstract IObservable<string> WhenNameUpdated();


        IObservable<ConnectionStatus> connOb;
        public virtual IObservable<ConnectionStatus> CreateConnection()
        {
            this.connOb = this.connOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                var state = this
                    .WhenStatusChanged()
                    .Subscribe(async status =>
                    {
                        try
                        {
                            ob.OnNext(status);
                            if (status == ConnectionStatus.Disconnected)
                                await this.Connect();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error connecting to device - " + ex);
                        }
                    });

                return () =>
                {
                    state.Dispose();
                    this.Disconnect();
                };
            })
            .Replay(1)
            .RefCount();

            return this.connOb;
        }
    }
}
