using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.OS;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    // TODO: wrap connection state events and call gatt.close() every disconnect state
    public class Device : AbstractDevice
    {
        readonly Subject<ConnectionStatus> connSubject;
        readonly Subject<GattStatus> connFailSubject;
        readonly BluetoothManager manager;
        readonly DeviceContext context;
        IDisposable autoReconnectSub;


        public Device(BluetoothManager manager,
                      BluetoothDevice native,
                      GattCallbacks callbacks) : base(native.Name, ToDeviceId(native.Address))
        {
            this.connSubject = new Subject<ConnectionStatus>();
            this.connFailSubject = new Subject<GattStatus>();
            this.context = new DeviceContext(native, callbacks);
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

            var sub1 = this.WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Connected)
                .Subscribe(_ =>
                {
                    connected = true;
                    if (config.IsPersistent)
                        this.autoReconnectSub = this.CreateAutoReconnectSubscription(config);

                    ob.Respond(null);
                });

            var sub2 = this.connFailSubject.Subscribe(x =>
            {
                this.connSubject.OnNext(ConnectionStatus.Disconnected);
                this.context.Gatt?.Close();
                ob.OnError(new Exception("Connection failed - " + x));
            });

            this.connSubject.OnNext(ConnectionStatus.Connecting);
            await this.context.Connect(config.Priority, config.AutoConnect);

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


        public override void CancelConnection()
        {
            this.connSubject.OnNext(ConnectionStatus.Disconnecting);
            this.autoReconnectSub?.Dispose();
            this.context.Close();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
        {
            var uuid = serviceUuid.ToUuid();
            var nativeService = context.Gatt.GetService(uuid);
            // If native service is null, it may be because the underlying
            // BT library hasn't yet discovered all the services on the device
            if (nativeService == null)
            {
                return this
                .WhenServiceDiscovered()
                .Where(x => x.Uuid.Equals(serviceUuid))
                .Take(1)
                .Select(x => x);
            }
            else
            {
                var service = new GattService(this, context, nativeService);
                return Observable.Return<IGattService>(service);
            }
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
                var sub1 = this.connSubject.Subscribe(ob.OnNext);
                var sub2 = this.context
                    .Callbacks
                    .ConnectionStateChanged
                    .Where(args => args.Gatt.Device.Equals(this.context.NativeDevice))
                    .Subscribe(args =>
                    {
                        if (args.Status != GattStatus.Success)
                            this.connFailSubject.OnNext(args.Status);

                        // if failed, likely no reason to broadcast this
                        ob.OnNext(this.Status);
                    });

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
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
                var sub1 = this.context
                    .Callbacks
                    .ServicesDiscovered
                    .Where(x => x.Gatt.Device.Equals(this.context.NativeDevice))
                    .Subscribe(args =>
                    {
                        foreach (var ns in args.Gatt.Services)
                        {
                            var service = new GattService(this, this.context, ns);
                            ob.OnNext(service);
                        }
                    });

                var sub2 = this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)

                    // this helps alleviate gatt 133 error
                    .Delay(CrossBleAdapter.AndroidPauseBeforeServiceDiscovery)
                    .Subscribe(_ => this.context.Gatt.DiscoverServices());

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
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

            var sub1 = this.context
                .Callbacks
                .ReadRemoteRssi
                .Where(x =>
                    x.Gatt.Device.Equals(this.context.NativeDevice) &&
                    x.IsSuccessful
                )
                .Subscribe(x => ob.OnNext(x.Rssi));

            var sub2 = this
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
                sub1.Dispose();
                sub2.Dispose();
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
                this.context
                    .Callbacks
                    .MtuChanged
                    .Where(x => x.Gatt.Equals(this.context.Gatt))
                    .Subscribe(x =>
                    {
                        this.currentMtu = x.Mtu;
                        ob.OnNext(x.Mtu);
                    })
            )
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
            .Select(_ => Observable.FromAsync(ct1 => this.DoReconnect(config, ct1)))
            .Merge()
            .Subscribe();


        async Task DoReconnect(GattConnectionConfig config, CancellationToken ct)
        {
            Log.Debug("Reconnect", "Starting reconnection loop");
            this.connSubject.OnNext(ConnectionStatus.Connecting);
            var attempts = 1;

            while (!ct.IsCancellationRequested &&
                   this.Status != ConnectionStatus.Connected &&
                   attempts <= CrossBleAdapter.AndroidMaxAutoReconnectAttempts)
            {
                Log.Write("Reconnect", "Reconnection Attempt " + attempts);

                // breathe before attempting (again)
                await Task.Delay(
                    CrossBleAdapter.AndroidPauseBetweenAutoReconnectAttempts,
                    ct
                );
                try
                {
                    await this.context.Reconnect(config.Priority);
                }
                catch (Exception ex)
                {
                    Log.Warn("Reconnect", "Error reconnecting " + ex);
                }
                attempts++;
            }
            if (this.Status != ConnectionStatus.Connected)
                await this.DoFallbackReconnect(config, ct);
        }


        async Task DoFallbackReconnect(GattConnectionConfig config, CancellationToken ct)
        {
            if (this.Status == ConnectionStatus.Connected)
            {
                Log.Debug("Reconnect", "Reconnection successful");
            }
            else
            {
                this.context.Close(); // kill current gatt

                if (ct.IsCancellationRequested)
                {
                    Log.Debug("Reconnect", "Reconnection loop cancelled");
                }
                else
                {
                    Log.Debug("Reconnect", "Reconnection failed - handing off to android autoReconnect");
                    try
                    {
                        await this.context.Connect(config.Priority, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Reconnect", "Reconnection failed to hand off - " + ex);
                    }
                }

            }
        }

        #endregion
    }
}