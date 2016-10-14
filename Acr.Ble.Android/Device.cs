using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acr.Ble.Internals;
using Android.App;
using Android.Bluetooth;
using Android.OS;
using B = Android.OS.Build;

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
            })
            .Replay(1)
            .RefCount();
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
                if (this.Status == ConnectionStatus.Connected)
                {
                    ob.Respond(null);
                    return Disposable.Empty;
                }
                var connected = this
                    .WhenStatusChanged()
                    .Take(1)
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ => ob.Respond(null));

                if (this.Status != ConnectionStatus.Connecting)
                {
                    try
                    {
                        ob.OnNext(ConnectionStatus.Connecting);
                        var conn = this.native.ConnectGatt(Application.Context, false, this.callbacks);
                        this.context = new GattContext(conn, this.callbacks);

                        if (this.ConnectOnMainThread()) 
                        {
                            Task.Factory.StartNew(
                                () => conn.Connect(), // this could still fire even if we cancel it thereby tying up the connection
                                CancellationToken.None,
                                TaskCreationOptions.None,
                                this.scheduler
                            );
                        }
                        else 
                        {
                            conn.Connect();
                        }
                    }
                    catch (Exception ex)
                    {
                        ob.OnNext(ConnectionStatus.Disconnected);
                        ob.OnError(ex);
                    }
                }
                return connected;
            });
        }


        public override void Disconnect()
        {
            if (this.Status != ConnectionStatus.Connected)
                return;

            this.connectObserver.OnNext(ConnectionStatus.Disconnecting);
            this.context?.Dispose();
            this.connectObserver.OnNext(ConnectionStatus.Disconnected);
        }


        public override IObservable<string> WhenNameUpdated()
        {
            return BluetoothObservables
                .WhenDeviceNameChanged()
                .Where(x => x.Equals(this.native))
                .Select(x => this.Name);
        }


        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            return this.connectOb;
        }


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
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
                    .Subscribe(_ => this.context.Gatt.DiscoverServices());

                return () =>
                {
                    sub.Dispose();
                    this.callbacks.ServicesDiscovered -= handler;
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
                IDisposable timer = null;
                var handler = new EventHandler<GattRssiEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Device.Equals(this.native))
                        ob.OnNext(args.Rssi);
                });
                this.context.Callbacks.ReadRemoteRssi += handler;

                var sub = this
                    .WhenStatusChanged()
                    .Subscribe(x =>
                    {
                        if (x == ConnectionStatus.Connected)
                        {
                            timer = Observable
                                .Interval(ts)
                                .Subscribe(_ => this.context.Gatt.ReadRemoteRssi());
                        }
                        else
                        {
                            timer?.Dispose();
                        }
                    });

                return () =>
                {
                    timer?.Dispose();
                    sub.Dispose();
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


        protected virtual bool ConnectOnMainThread() 
        {
            if (AndroidConfig.ForceConnectOnMainThread)
                return true;
            
            if (!B.Manufacturer.Equals("samsung", StringComparison.CurrentCultureIgnoreCase))
                return false;

            if (B.VERSION.SdkInt > BuildVersionCodes.Kitkat)
                return false;

            return true;
        }
    }
}