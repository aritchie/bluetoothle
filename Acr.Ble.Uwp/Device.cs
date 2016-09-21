using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;


namespace Acr.Ble
{
    public class Device : IDevice
    {
        readonly IList<IGattService> services = new List<IGattService>();
        readonly GattDeviceService native;


        public Device(GattDeviceService native)
        {
            this.native = native;
        }


        public string Name => this.native.Device.Name;
        public Guid Uuid => this.native.Uuid;


        public IObservable<ConnectionStatus> CreateConnection()
        {
            return Observable.Create<ConnectionStatus>(async ob =>
            {
                var status = this
                    .WhenStatusChanged()
                    .Subscribe(ob.OnNext);
                await this.Connect();

                return status;
            });
        }


        public IObservable<object> Connect()
        {
            return Observable.Empty<object>();
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            return Observable.Empty<int>();
        }


        public void Disconnect()
        {
        }


        public ConnectionStatus Status
        {
            get
            {
                switch (this.native.Device.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        IObservable<ConnectionStatus> statusOb;
        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                this.native.Device.ConnectionStatusChanged += handler;
                return () => this.native.Device.ConnectionStatusChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
                this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(x =>
                    {
                        this.services.Clear();
                        foreach (var nservice in this.native.Device.GattServices)
                        {
                            var service = new GattService(nservice, this);
                            ob.OnNext(service);
                        }
                    })
            )
            .Publish()
            .RefCount();

            return this.serviceOb;
        }


        IObservable<string> nameOb;
        public IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);

                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                this.native.Device.NameChanged += handler;
                return () => this.native.Device.NameChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }
    }
}