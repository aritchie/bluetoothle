using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Acr.Logging;
using Acr.Reactive;
using CoreBluetooth;
using Foundation;


namespace Plugin.BluetoothLE
{
    public partial class Device : AbstractDevice
    {
        readonly AdapterContext context;
        readonly CBPeripheral peripheral;
        IDisposable autoReconnectSub;


        public Device(AdapterContext context, CBPeripheral peripheral) : base(peripheral.Name,
            peripheral.Identifier.ToGuid())
        {
            this.context = context;
            this.peripheral = peripheral;
        }


        public CBPeripheral Peripheral => this.peripheral;
        public override object NativeDevice => this.peripheral;


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


        public override void Connect(ConnectionConfig config)
        {
            var arc = config?.AutoConnect ?? true;
            if (arc)
            {
                this.autoReconnectSub = this
                    .WhenDisconnected()
                    .Skip(1)
                    .Subscribe(_ => this.DoConnect());
            }
            this.DoConnect();
        }


        protected void DoConnect() => this.context
            .Manager
            .ConnectPeripheral(this.peripheral, new PeripheralConnectionOptions
            {
                NotifyOnDisconnection = true,
#if __IOS__ || __TVOS__
                NotifyOnConnection = true,
                NotifyOnNotification = true
#endif
            });


        public override void CancelConnection()
        {
            this.autoReconnectSub?.Dispose();
            this.context
                .Manager
                .CancelPeripheralConnection(this.peripheral);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.context
            .FailedConnection
            .Where(x => x.Peripheral.Equals(this.peripheral))
            .Select(x => new BleException(x.Error.ToString()));


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

                var sub1 = this.context
                    .PeripheralConnected
                    .Where(x => x.Equals(this.peripheral))
                    .Subscribe(x => ob.OnNext(this.Status));

                //var sub = this.context
                //    .FailedConnection
                //    .Where(x => x.Equals(this.peripheral))
                //    .Subscribe(x => ob.OnNext(ConnectionStatus.Failed));

                var sub2 = this.context
                    .PeripheralDisconnected
                    .Where(x => x.Equals(this.peripheral))
                    .Subscribe(x => ob.OnNext(this.Status));

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => Observable.Create<IGattService>(ob =>
            {
                var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
                {
                    if (this.peripheral.Services == null)
                        return;

                    foreach (var native in this.peripheral.Services)
                    {
                        var service = new GattService(this, native);
                        if (service.Uuid.Equals(serviceUuid))
                        {
                            ob.Respond(service);
                            break;
                        }
                    }
                });
                this.peripheral.DiscoveredService += handler;
                this.peripheral.DiscoverServices(new[] {serviceUuid.ToCBUuid()});

                return () => this.peripheral.DiscoveredService -= handler;
            });


        public override IObservable<IGattService> DiscoverServices() => Observable.Create<IGattService>(ob =>
        {
            Log.Info(BleLogCategory.Device, "service discovery hooked for device " + this.Uuid);
            var services = new Dictionary<Guid, IGattService>();

            var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
            {
                if (args.Error != null)
                {
                    ob.OnError(new BleException(args.Error.LocalizedDescription));
                    return;
                }

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
                ob.OnCompleted();
            });
            this.peripheral.DiscoveredService += handler;
            this.peripheral.DiscoverServices();

            return () => this.peripheral.DiscoveredService -= handler;
        });


        public override IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
        {
            var handler = new EventHandler<CBRssiEventArgs>((sender, args) =>
            {
                if (args.Error == null)
                    ob.Respond(args.Rssi?.Int32Value ?? 0);
                else
                    ob.OnError(new Exception(args.Error.LocalizedDescription));
            });
            this.peripheral.RssiRead += handler;
            this.peripheral.ReadRSSI();

            return () => this.peripheral.RssiRead += handler;
        });


        public override int GetHashCode() => this.peripheral.GetHashCode();


        public override bool Equals(object obj)
        {
            var other = obj as Device;
            if (other == null)
                return false;

            if (!this.peripheral.Equals(other.peripheral))
                return false;

            return false;
        }


        public override string ToString() => this.Uuid.ToString();
    }
}