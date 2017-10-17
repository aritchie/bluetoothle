using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.OS;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly Subject<ConnectionStatus> connSubject;
        readonly Subject<GattStatus> connFailSubject;
        readonly BluetoothManager manager;
        readonly GattContext context;
        IDisposable autoReconnectSub;


        public Device(BluetoothManager manager,
                      BluetoothDevice native,
                      GattCallbacks callbacks) : base(native.Name, ToDeviceId(native.Address))
        {
            this.connSubject = new Subject<ConnectionStatus>();
            this.connFailSubject = new Subject<GattStatus>();
            this.context = new GattContext(native, callbacks);
            this.manager = manager;
        }


        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.All;


        public override ConnectionStatus Status
        {
            get
            {
                var state = this.manager.GetConnectionState(this.context.NativeDevice, ProfileType.Gatt);
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


        public override IObservable<object> Connect(GattConnectionConfig config) => Observable.Create<object>(async ob =>
        {
            var connected = false;
            config = config ?? GattConnectionConfig.DefaultConfiguration;
            if (config.IsPersistent)
                this.autoReconnectSub = this.CreateAutoReconnectSubscription(config);

            var sub1 = this.WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Connected)
                .Subscribe(_ =>
                {
                    connected = true;
                    ob.Respond(null);
                });

            var sub2 = this.connFailSubject.Subscribe(x =>
            {
                this.connSubject.OnNext(ConnectionStatus.Disconnected);
                this.context.Gatt?.Close();
                ob.OnError(new Exception("Connection failed - " + x));
            });

            this.connSubject.OnNext(ConnectionStatus.Connecting);
            await this.context.Connect(config.Priority, false);

            return () =>
            {
                if (!connected)
                {
                    this.context.Gatt?.Close();
                    this.connSubject.OnNext(ConnectionStatus.Disconnected);
                }
                sub1.Dispose();
                sub2.Dispose();
            };
        });


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => this
            .WhenServiceDiscovered()
            .Where(x => x.Uuid.Equals(serviceUuid))
            .Take(1)
            .Select(x => x);


        public override void CancelConnection()
        {
            this.connSubject.OnNext(ConnectionStatus.Disconnecting);
            this.autoReconnectSub?.Dispose();
            this.context.Close();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        public override IObservable<string> WhenNameUpdated() => BluetoothObservables
            .WhenDeviceNameChanged()
            .Where(x => x.Equals(this.context.NativeDevice))
            .Select(x => this.Name);


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                var sub = this.connSubject.Subscribe(ob.OnNext);
                var handler = new EventHandler<ConnectionStateEventArgs>((sender, args) =>
                {
                    if (!args.Gatt.Device.Equals(this.context.NativeDevice))
                        return;

                    if (args.Status != GattStatus.Success)
                        this.connFailSubject.OnNext(args.Status);

                    // if failed, likely no reason to broadcast this
                    ob.OnNext(this.Status);
                });
                this.context.Callbacks.ConnectionStateChanged += handler;

                return () =>
                {
                    sub.Dispose();
                    this.context.Callbacks.ConnectionStateChanged -= handler;
                };
            })
            .StartWith(this.Status)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
            {
                var handler = new EventHandler<GattEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Device.Equals(this.context.NativeDevice))
                    {
                        foreach (var ns in args.Gatt.Services)
                        {
                            var service = new GattService(this, this.context, ns);
                            ob.OnNext(service);
                        }
                    }
                });
                this.context.Callbacks.ServicesDiscovered += handler;
                var sub = this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ =>
                    {
                        Thread.Sleep(1000); // this helps alleviate gatt 133 error
                        this.context.Gatt.DiscoverServices();
                    });

                return () =>
                {
                    sub.Dispose();
                    this.context.Callbacks.ServicesDiscovered -= handler;
                };
            })
            .ReplayWithReset(this
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.serviceOb;
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan) => Observable.Create<int>(ob =>
        {
            var ts = timeSpan ?? TimeSpan.FromSeconds(3);
            IDisposable timer = null;
            var handler = new EventHandler<GattRssiEventArgs>((sender, args) =>
            {
                if (args.Gatt.Device.Equals(this.context.NativeDevice) && args.IsSuccessful)
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


        public override IObservable<bool> PairingRequest(string pin) => Observable.Create<bool>(ob =>
        {
            IDisposable requestOb = null;
            IDisposable istatusOb = null;

            if (this.PairingStatus == PairingStatus.Paired)
            {
                ob.Respond(true);
            }
            else
            {
                if (pin != null && Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    requestOb = BluetoothObservables
                        .WhenBondRequestReceived()
                        .Where(x => x.Equals(this.context.NativeDevice))
                        .Subscribe(x =>
                        {
                            var bytes = ConvertPinToBytes(pin);
                            x.SetPin(bytes);
                            x.SetPairingConfirmation(true);
                        });
                }
                istatusOb = BluetoothObservables
                    .WhenBondStatusChanged()
                    .Where(x => x.Equals(this.context.NativeDevice) && x.BondState != Bond.Bonding)
                    .Subscribe(x => ob.Respond(x.BondState == Bond.Bonded)); // will complete here

                // execute
                this.context.NativeDevice.CreateBond();
            }
            return () =>
            {
                requestOb?.Dispose();
                istatusOb?.Dispose();
            };
        });


        public override IGattReliableWriteTransaction BeginReliableWriteTransaction() =>
            new GattReliableWriteTransaction(this.context);


        public static byte[] ConvertPinToBytes(string pin)
        {
            var bytes = new List<byte>();
            foreach (var p in pin)
            {
                if (!char.IsDigit(p))
                    throw new ArgumentException("PIN contain invalid value - " + p);

                var value = byte.Parse(p.ToString());
                if (value > 10)
                    throw new ArgumentException("Invalid range for PIN value - " + value);

                bytes.Add(value);
            }
            return bytes.ToArray();
        }


        public override PairingStatus PairingStatus
        {
            get
            {
                switch (this.context.NativeDevice.BondState)
                {
                    case Bond.Bonded:
                        return PairingStatus.Paired;

                    default:
                    case Bond.None:
                        return PairingStatus.NotPaired;
                }
            }
        }


        int currentMtu = 20;
        public override IObservable<int> RequestMtu(int size)
        {
            if (!this.IsMtuRequestAvailable())
                return base.RequestMtu(size);

            return Observable.Create<int>(ob =>
            {
                var sub1 = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(x => this.context.Gatt.RequestMtu(size));

                var sub2 = this.WhenMtuChanged()
                    .Take(1)
                    .Subscribe(ob.Respond);

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            });
        }


        IObservable<int> mtuOb;
        public override IObservable<int> WhenMtuChanged()
        {
            this.mtuOb = this.mtuOb ?? Observable.Create<int>(ob =>
            {
                var handler = new EventHandler<MtuChangedEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Equals(this.context.Gatt))
                    {
                        this.currentMtu = args.Mtu;
                        ob.OnNext(args.Mtu);
                    }
                });
                var sub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(_ =>
                    {
                        ob.OnNext(this.currentMtu);
                        this.context.Callbacks.MtuChanged += handler;
                    });

                return () =>
                {
                    sub.Dispose();
                    if (this.context?.Callbacks != null)
                        this.context.Callbacks.MtuChanged -= handler;
                };
            })
            .Replay(1)
            .RefCount();

            return this.mtuOb;
        }


        public override int GetCurrentMtuSize() => this.currentMtu;
        public override int GetHashCode() => this.context.NativeDevice.GetHashCode();


        public override bool Equals(object obj)
        {
            var other = obj as Device;
            if (other == null)
                return false;

            if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override string ToString() => $"Device: {this.Uuid}";

        #region Internals

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


        // TODO: do I need to watch for connection errors here?
        IDisposable CreateAutoReconnectSubscription(GattConnectionConfig config) => this
            .WhenStatusChanged()
            .Skip(1) // skip the initial "Disconnected"
            .Where(x => x == ConnectionStatus.Disconnected)
            .Select(_ => Observable.FromAsync(ct1 => this.Reconnect(config, ct1)))
            .Merge()
            .Subscribe();


        async Task Reconnect(GattConnectionConfig config, CancellationToken ct)
        {
            Log.Write("Starting reconnection loop");
            this.connSubject.OnNext(ConnectionStatus.Connecting);
            var attempts = 1;

            while (!ct.IsCancellationRequested &&
                   this.Status != ConnectionStatus.Connected &&
                   attempts <= CrossBleAdapter.AndroidMaxAutoReconnectAttempts)
            {
                Log.Write("Reconnection Attempt " + attempts);

                // breathe before attempting (again)
                await Task.Delay(
                    CrossBleAdapter.AndroidPauseBetweenAutoReconnectAttempts,
                    ct
                );
                await this.context.Reconnect(config.Priority);
                attempts++;
            }
            if (ct.IsCancellationRequested)
            {
                Log.Write("Reconnection loop cancelled");
            }
            else if (this.Status == ConnectionStatus.Connected)
            {
                Log.Write("Reconnection successful");
            }
            else
            {
                Log.Write("Reconnection failed - handing off to android autoReconnect");
                this.context.Close();
                await this.context.Connect(config.Priority, true);
            }
        }

        #endregion
    }
}