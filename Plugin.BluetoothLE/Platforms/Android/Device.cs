using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Acr;
using Android.Bluetooth;
using Android.OS;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly Subject<ConnectionStatus> connSubject;
        readonly BluetoothManager manager;
        readonly DeviceContext context;
        IDisposable autoReconnectSub;


        public Device(BluetoothManager manager,
                      BluetoothDevice native,
                      GattCallbacks callbacks) : base(native.Name, ToDeviceId(native.Address))
        {
            this.connSubject = new Subject<ConnectionStatus>();
            this.context = new DeviceContext(native, callbacks);
            this.manager = manager;
        }


        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.All;


        public override ConnectionStatus Status => this
            .manager
            .GetConnectionState(this.context.NativeDevice, ProfileType.Gatt)
            .ToStatus();


        public override void Connect(GattConnectionConfig config)
        {
            config = config ?? GattConnectionConfig.DefaultConfiguration;
            if (config.IsPersistent)
            {
                this.autoReconnectSub = this.WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Disconnected)
                    .Skip(1)
                    .Delay(CrossBleAdapter.PauseBetweenAutoReconnectAttempts)
                    .Subscribe(_ =>
                    {
                        // TODO: watch for GATT 133 for retry
                        this.context.Connect(config.Priority, true);
                    });
            }
            this.connSubject.OnNext(ConnectionStatus.Connecting);
            this.context.Connect(config.Priority, config.AndroidAutoConnect);
        }


        public override void CancelConnection()
        {
            this.autoReconnectSub?.Dispose();
            this.context.Close();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => this
            .DiscoverServices()
            .Where(x => x.Uuid.Equals(serviceUuid))
            .Take(1)
            .Select(x => x);


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
                    .Select(x => x.NewState.ToStatus())
                    .DistinctUntilChanged()
                    .Subscribe(ob.OnNext);

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            })
            .StartWith(this.Status)
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> DiscoverServices()
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
                    .WhenConnected()

                    // this helps alleviate gatt 133 error
                    .Delay(CrossBleAdapter.PauseBeforeServiceDiscovery)
                    .Subscribe(_ =>
                    {
                        if (!this.context.Gatt.DiscoverServices())
                            ob.OnError(new Exception("Failed to request services"));
                    });

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
                if (!this.context.NativeDevice.CreateBond())
                    ob.Respond(false);
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


        public override int MtuSize => this.currentMtu;
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


        //// TODO: do I need to watch for connection errors here?
        //IDisposable CreateAutoReconnectSubscription(GattConnectionConfig config) => this
        //    .WhenStatusChanged()
        //    .Skip(1) // skip the initial "Disconnected"
        //    .Where(x => x == ConnectionStatus.Disconnected)
        //    .Select(_ => Observable.FromAsync(ct1 => this.DoReconnect(config, ct1)))
        //    .Merge()
        //    .Subscribe();


        //async Task DoReconnect(GattConnectionConfig config, CancellationToken ct)
        //{
        //    Log.Debug("Reconnect", "Starting reconnection loop");
        //    this.connSubject.OnNext(ConnectionStatus.Connecting);
        //    var attempts = 1;

        //    while (!ct.IsCancellationRequested &&
        //           this.Status != ConnectionStatus.Connected &&
        //           attempts <= AndroidBleConfiguration.MaxAutoReconnectAttempts)
        //    {
        //        Log.Write("Reconnect", "Reconnection Attempt " + attempts);

        //        // breathe before attempting (again)
        //        await Task.Delay(
        //            AndroidBleConfiguration.PauseBetweenAutoReconnectAttempts,
        //            ct
        //        );
        //        try
        //        {
        //            //await this.context.Reconnect(config.Priority);
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Warn("Reconnect", "Error reconnecting " + ex);
        //        }
        //        attempts++;
        //    }
        //    if (this.Status != ConnectionStatus.Connected)
        //        await this.DoFallbackReconnect(config, ct);
        //}


        //async Task DoFallbackReconnect(GattConnectionConfig config, CancellationToken ct)
        //{
        //    if (this.Status == ConnectionStatus.Connected)
        //    {
        //        Log.Debug("Reconnect", "Reconnection successful");
        //    }
        //    else
        //    {
        //        this.context.Close(); // kill current gatt

        //        if (ct.IsCancellationRequested)
        //        {
        //            Log.Debug("Reconnect", "Reconnection loop cancelled");
        //        }
        //        else
        //        {
        //            Log.Debug("Reconnect", "Reconnection failed - handing off to android autoReconnect");
        //            try
        //            {
        //                //await this.context.Connect(config.Priority, true);
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Error("Reconnect", "Reconnection failed to hand off - " + ex);
        //            }
        //        }

        //    }
        //}

        #endregion
    }
}