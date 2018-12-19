using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Android.OS;
using Acr.Logging;
using Acr.Reactive;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly Subject<ConnectionStatus> connSubject;
        readonly BluetoothManager manager;
        readonly DeviceContext context;


        public Device(BluetoothManager manager, BluetoothDevice native)
            : base(native.Name, ToDeviceId(native.Address))
        {
            this.connSubject = new Subject<ConnectionStatus>();
            this.context = new DeviceContext(native);
            this.manager = manager;
        }


        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.All;


        public override ConnectionStatus Status => this
            .manager
            .GetConnectionState(this.context.NativeDevice, ProfileType.Gatt)
            .ToStatus();


        public override void Connect(ConnectionConfig config)
        {
            this.connSubject.OnNext(ConnectionStatus.Connecting);
            this.context.Connect(config);
        }


        public override void CancelConnection()
        {
            this.context.Close();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.context.ConnectionFailed;
        //public override IObservable<BleException> WhenConnectionFailed() => this.context
        //    .Callbacks
        //    .ConnectionStateChanged
        //    .Where(x => !x.IsSuccessful)
        //    .Select(x => new BleException($"Failed to connect to device - {x.Status}"));


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


        public override IObservable<ConnectionStatus> WhenStatusChanged()
            => Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status); // won't return connecting/disconnecting states
                var sub1 = this.connSubject.Subscribe(ob.OnNext);
                var sub2 = this.context
                    .Callbacks
                    .ConnectionStateChanged
                    .Where(x => x.Gatt.Device.Address.Equals(this.context.NativeDevice.Address))
                    .Select(x =>
                    {
                        Log.Info(BleLogCategory.Device, "Android Connection State: {x.NewState} - {x.Status}");
                        return x.NewState.ToStatus();
                    })
                    //.DistinctUntilChanged()
                    .Subscribe(ob.OnNext);

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            });


        public override IObservable<IGattService> DiscoverServices()
            => Observable.Create<IGattService>(ob =>
            {
                var sub = this.context
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
                        ob.OnCompleted();
                    });

                this.context.RefreshServices();
                this.context.Gatt.DiscoverServices();

                return sub;
            });


        public override IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
        {
            var sub = this.context
                .Callbacks
                .ReadRemoteRssi
                .Take(1)
                .Subscribe(x => ob.Respond(x.Rssi));

            this.context.Gatt?.ReadRemoteRssi();
            //if (this.context.Gatt?.ReadRemoteRssi() ?? false)
            //    ob.OnError(new BleException("Failed to read RSSI"));

            return sub;
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
                        },
                        ob.OnError);
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
        public override IObservable<int> RequestMtu(int size) => Observable.Create<int>(ob =>
        {
            var sub = this.WhenMtuChanged().Skip(1).Subscribe(ob.Respond);
            this.context.Gatt.RequestMtu(size);
            return sub;
        });


        public override IObservable<int> WhenMtuChanged() => this.context
            .Callbacks
            .MtuChanged
            .Where(x => x.Gatt.Equals(this.context.Gatt))
            .Select(x =>
            {
                this.currentMtu = x.Mtu;
                return x.Mtu;
            })
            .StartWith(this.currentMtu);


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