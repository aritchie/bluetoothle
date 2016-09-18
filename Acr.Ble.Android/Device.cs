using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.Ble.Internals;
using Android.App;
using Android.Bluetooth;


namespace Acr.Ble
{
    public class Device : AbstractDevice
    {
        readonly BluetoothManager manager;
        readonly BluetoothDevice native;
        readonly GattCallbacks callbacks;
        readonly TaskScheduler scheduler;
        readonly IObservable<ConnectionStatus> connectOb;
        IObserver<ConnectionStatus> connectObserver;
        GattContext context;


        public Device(BluetoothManager manager,
                      BluetoothDevice native,
                      GattCallbacks callbacks,
                      TaskScheduler scheduler) : base(native.Name, ToDeviceId(native.Address))
        {
            this.manager = manager;
            this.native = native;
            this.callbacks = callbacks;
            this.scheduler = scheduler; // this is the thread that the device was scanned on (required by some devices)

            this.connectOb = Observable.Create<ConnectionStatus>(ob =>
            {
                this.connectObserver = ob;

                var handler = new EventHandler<ConnectionStateEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Device.Equals(this.native))
                        ob.OnNext(this.Status);
                });
                this.callbacks.ConnectionStateChanged += handler;

                return () => this.callbacks.ConnectionStateChanged -= handler;
            });
        }


        public override ConnectionStatus Status
        {
            get
            {
                var state = this.manager.GetConnectionState(this.native, ProfileType.Gatt);
                switch (state)
                {
                    case ProfileState.Connected:
                        return ConnectionStatus.Connected;

                    case ProfileState.Connecting:
                        return ConnectionStatus.Connecting;

                    case ProfileState.Disconnecting:
                        return ConnectionStatus.Disconnecting;

                    case ProfileState.Disconnected:
                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        public override IObservable<object> Connect()
        {
            return Observable.Create<object>(ob =>
            {
                var connected = this
                    .WhenStatusChanged()
                    .Take(1)
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ =>
                    {
                        ob.OnNext(null);
                        ob.OnCompleted();
                    });

                var conn = this.native.ConnectGatt(Application.Context, true, this.callbacks);
                this.context = new GattContext(conn, this.callbacks);

                return connected;
            });
        }


        public override void Disconnect()
        {
            if (this.context == null)
                return;

            this.context?.Dispose();
            this.context = null;
            this.connectObserver.OnNext(ConnectionStatus.Disconnected);
        }


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? BluetoothObservables
                .WhenDeviceNameChanged()
                .Where(x => x.Equals(this.native))
                .Select(x => this.Name)
                .Publish()
                .RefCount();

            return this.nameOb;
        }


        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            return this.connectOb;
        }


        IObservable<IGattService> servicesOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.servicesOb = this.servicesOb ?? Observable.Create<IGattService>(ob =>
            {
                var handler = new EventHandler<GattEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Device.Equals(this.native))
                    {
                        foreach (var ns in args.Gatt.Services)
                        {
                            var service = new GattService(this, this.context, ns);
                            ob.OnNext(service);
                        }
                    }
                });
                this.callbacks.ServicesDiscovered += handler;

                var sub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ =>
                    {
                        this.Services.Clear();
                        this.context.Gatt.DiscoverServices();
                    });

                return () =>
                {
                    this.callbacks.ServicesDiscovered -= null;
                    sub.Dispose();
                };
            })
            .Publish()
            .RefCount();

            return this.servicesOb;
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan)
        {
            var ts = timeSpan ?? TimeSpan.FromSeconds(3);

            return Observable.Create<int>(ob =>
            {
                var handler = new EventHandler<GattRssiEventArgs>((sender, args) => ob.OnNext(args.Rssi));
                this.context.Callbacks.ReadRemoteRssi += handler;
                var innerOb = Observable
                    .Interval(ts)
                    .Where(x => this.Status == ConnectionStatus.Connected)
                    .Subscribe(_ => this.context.Gatt.ReadRemoteRssi());

                return () =>
                {
                    innerOb.Dispose();
                    this.context.Callbacks.ReadRemoteRssi -= handler;
                };
            });
        }


        // thanks monkey robotics
        protected static Guid ToDeviceId(string address)
        {
            var deviceGuid = new byte[16];
            var mac = address.Replace(":", "");
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();
            macBytes.CopyTo(deviceGuid, 10);
            return new Guid(deviceGuid);
        }
    }
}