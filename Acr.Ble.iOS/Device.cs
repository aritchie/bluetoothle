using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;


namespace Acr.Ble
{
    public class Device : AbstractDevice
    {
        readonly CBCentralManager manager;
        readonly CBPeripheral peripheral;
        readonly Subject<IGattService> subject;


        public Device(CBCentralManager manager, CBPeripheral peripheral) : base(peripheral.Name, peripheral.Identifier.ToGuid())
        {
            this.manager = manager;
            this.peripheral = peripheral;
            this.subject = new Subject<IGattService>();

            this.WhenStatusChanged()
                .Subscribe(x =>
                {
                    switch (x)
                    {
                        case ConnectionStatus.Connected:
                            this.Services.Clear();
                            this.peripheral.DiscoveredService += this.OnServiceDiscovered;
                            this.peripheral.DiscoverServices();
                            break;

                        case ConnectionStatus.Disconnected:
                            this.peripheral.DiscoveredService -= this.OnServiceDiscovered;
                            this.Services.Clear();
                            break;
                    }
                });

        }


        public override ConnectionStatus Status
        {
            get
            {
                switch (this.peripheral.State)
                {
                    case CBPeripheralState.Connected:
                        return ConnectionStatus.Connected;

                    case CBPeripheralState.Connecting:
                        return ConnectionStatus.Connecting;

                    case CBPeripheralState.Disconnecting:
                        return ConnectionStatus.Disconnecting;

                    case CBPeripheralState.Disconnected:
                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        public override IObservable<object> Connect()
        {
            return Observable.Create<object>(ob =>
            {
                var error = new EventHandler<CBPeripheralErrorEventArgs>((sender, args) =>
                {
                    if (args.Peripheral.Equals(this.peripheral))
                        ob.OnError(new Exception(args.Error.LocalizedDescription));
                });
                var connect = new EventHandler<CBPeripheralEventArgs>((sender, args) =>
                {
                    if (args.Peripheral.Equals(this.peripheral))
                    {
                        ob.OnNext(new object());
                        ob.OnCompleted();
                    }
                });

                this.manager.ConnectedPeripheral += connect;
                this.manager.FailedToConnectPeripheral += error;

                this.manager.ConnectPeripheral(this.peripheral, new PeripheralConnectionOptions
                {
                    NotifyOnConnection = true,
                    NotifyOnDisconnection = true,
                    NotifyOnNotification = true
                });

                return () =>
                {
                    this.manager.ConnectedPeripheral -= connect;
                    this.manager.FailedToConnectPeripheral -= error;
                };
            });
        }


        public override void Disconnect()
        {
            this.manager.CancelPeripheralConnection(this.peripheral);
        }


        public override IObservable<string> WhenNameUpdated()
        {
            return Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);
                var handler = new EventHandler((sender, args) => ob.OnNext(this.Name));
                this.peripheral.UpdatedName += handler;

                return () => this.peripheral.UpdatedName -= handler;
            });
        }


        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            return Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);

                var chandler = new EventHandler<CBPeripheralEventArgs>((sender, args) =>
                {
                    if (args.Peripheral.Equals(this.peripheral))
                        ob.OnNext(this.Status);
                });
                var dhandler = new EventHandler<CBPeripheralErrorEventArgs>((sender, args) =>
                {
                    if (args.Peripheral.Equals(this.peripheral))
                        ob.OnNext(this.Status);
                });
                var error = new EventHandler<CBPeripheralErrorEventArgs>((sender, args) =>
                {
                    if (args.Peripheral.Equals(this.peripheral))
                        ob.OnNext(this.Status);
                        //ob.OnError(new Exception(args.Error.ToString()));
                });
                this.manager.ConnectedPeripheral += chandler;
                this.manager.DisconnectedPeripheral += dhandler;
                this.manager.FailedToConnectPeripheral += error;

                return () =>
                {
                    this.manager.ConnectedPeripheral -= chandler;
                    this.manager.DisconnectedPeripheral -= dhandler;
                    this.manager.FailedToConnectPeripheral -= error;
                };
            });
        }


        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            return Observable.Create<IGattService>(ob =>
            {
                foreach (var service in this.Services.Values)
                    ob.OnNext(service);

                return this.subject.Subscribe(ob.OnNext);
            });
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan)
        {
            var ts = timeSpan ?? TimeSpan.FromSeconds(3);

            return Observable.Create<int>(ob =>
            {
                var handler = new EventHandler<CBRssiEventArgs>((sender, args) => ob.OnNext(args.Rssi?.Int32Value ?? 0));
                this.peripheral.RssiRead += handler;
                var innerOb = Observable
                    .Interval(ts)
                    .Where(x => this.Status == ConnectionStatus.Connected)
                    .Subscribe(_ => this.peripheral.ReadRSSI());

                return () =>
                {
                    innerOb.Dispose();
                    this.peripheral.RssiRead -= handler;
                };
            });
        }


        protected virtual void OnServiceDiscovered(object sender, NSErrorEventArgs args)
        {
            if (peripheral.Services == null)
                return;

            foreach (var native in peripheral.Services)
            {
                var service = new GattService(this, native);
                if (!this.Services.ContainsKey(service.Uuid))
                {
                    this.Services.Add(service.Uuid, service);
                    this.subject.OnNext(service);
                }
            }
        }
    }
}