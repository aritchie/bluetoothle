using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Acr.Ble
{
    public class Device : AbstractDevice
    {
        readonly CBCentralManager manager;
        readonly CBPeripheral peripheral;


        public Device(CBCentralManager manager, CBPeripheral peripheral) : base(peripheral.Name, peripheral.Identifier.ToGuid())
        {
            this.manager = manager;
            this.peripheral = peripheral;
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
                        ob.Respond(null);
                });

                this.manager.ConnectedPeripheral += connect;
                this.manager.FailedToConnectPeripheral += error;

                this.manager.ConnectPeripheral(this.peripheral, new PeripheralConnectionOptions
                {
                    NotifyOnDisconnection = true,
#if __IOS__
                    NotifyOnConnection = true,
                    NotifyOnNotification = true
#endif                        
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


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);
                var handler = new EventHandler((sender, args) => ob.OnNext(this.Name));
                this.peripheral.UpdatedName += handler;

                return () => this.peripheral.UpdatedName -= handler;
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
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
                        ob.OnError(new Exception(args.Error.ToString()));
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
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
            {
                Debug.WriteLine("Hooked for services for device " + this.Uuid);
                var services = new Dictionary<Guid, IGattService>();

                var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
                {
                    if (this.peripheral.Services == null)
                        return;

                    foreach (var native in this.peripheral.Services)
                    {
                        var service = new GattService(this, native);
                        if (!services.ContainsKey(service.Uuid))
                        {
                            services.Add(service.Uuid, service);
                            ob.OnNext(service);
                        }
                    }
                });
                this.peripheral.DiscoveredService += handler;

                var sub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ => 
                    {
                        this.peripheral.DiscoverServices();
                        Debug.WriteLine("DiscoverServices for device " + this.Uuid);
                    });

                return () => 
                {
                    sub.Dispose();
                    this.peripheral.DiscoveredService -= handler;
                };
            })
            .ReplayWithReset(this
                .WhenStatusChanged()
                .Skip(1)
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();
            
            return this.serviceOb;
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
    }
}