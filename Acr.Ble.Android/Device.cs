using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Acr.Ble.Internals;
using Android.App;
using Android.Bluetooth;
using Android.OS;


namespace Acr.Ble
{
    public class Device : AbstractDevice
    {
        readonly BluetoothManager manager;
        readonly BluetoothDevice native;
        readonly GattCallbacks callbacks;
        readonly TaskScheduler scheduler;
        readonly Subject<ConnectionStatus> connSubject;
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
            this.connSubject = new Subject<ConnectionStatus>();
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
                var cancelSrc = new CancellationTokenSource();
                IDisposable connected = null;

                if (this.Status == ConnectionStatus.Connected)
                {
                    ob.Respond(null);
                }
                else
                {
                    connected = this
                        .WhenStatusChanged()
                        .Take(1)
                        .Where(x => x == ConnectionStatus.Connected)
                        .Subscribe(_ =>
                        {
                            if (AndroidConfig.MaxTransmissionUnitSize != null)
                            {
                                this.context.Gatt.RequestMtu(AndroidConfig.MaxTransmissionUnitSize.Value);
                            }
                            ob.Respond(null);
                        });

                    if (this.Status != ConnectionStatus.Connecting)
                    {
                        try
                        {
                            ob.OnNext(ConnectionStatus.Connecting);
                            var conn = this.native.ConnectGatt(Application.Context, false, this.callbacks);
                            this.context = new GattContext(conn, this.callbacks);

                            switch (AndroidConfig.ConnectionThread)
                            {
                                case ConnectionThread.MainThread:
                                    Application.SynchronizationContext.Post(_ =>
                                    {
                                        conn.Connect();
                                    }, null);
                                    break;

                                case ConnectionThread.ScanThread:
                                    Task.Factory.StartNew(
                                        () => conn.Connect(), // TODO: if this crashes, need to junk out the observable
                                        // this could still fire even if we cancel it thereby tying up the connection
                                        cancelSrc.Token,
                                        TaskCreationOptions.None,
                                        this.scheduler
                                    );
                                    break;

                                case ConnectionThread.Default:
                                default:
                                    conn.Connect();
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            ob.OnNext(ConnectionStatus.Disconnected);
                            ob.OnError(ex);
                        }
                    }
                }
                return () =>
                {
                    connected?.Dispose();
                    cancelSrc.Dispose();
                };
            });
        }


        public override void Disconnect()
        {
            if (this.Status != ConnectionStatus.Connected)
                return;

            this.connSubject.OnNext(ConnectionStatus.Disconnecting);
            this.context?.Dispose();
            this.connSubject.OnNext(ConnectionStatus.Disconnected);
        }


        public override IObservable<string> WhenNameUpdated()
        {
            return BluetoothObservables
                .WhenDeviceNameChanged()
                .Where(x => x.Equals(this.native))
                .Select(x => this.Name);
        }


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new EventHandler<ConnectionStateEventArgs>((sender, args) =>
                {
                    if (args.Gatt.Device.Equals(this.native))
                        ob.OnNext(this.Status);
                });
                this.callbacks.ConnectionStateChanged += handler;
                var sub = this.connSubject.AsObservable().Subscribe(ob.OnNext);

                return () =>
                {
                    sub.Dispose();
                    this.callbacks.ConnectionStateChanged -= handler;
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
                        Thread.Sleep(1000); // this helps alleviate gatt 133 error
                        this.context.Gatt.DiscoverServices();
                    });

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


        public override bool IsPairingRequestSupported => true;


        public override IObservable<bool> PairingRequest(string pin)
        {
            return Observable.Create<bool>(ob =>
            {
                IDisposable requestOb = null;
                IDisposable statusOb = null;

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
                            .Where(x => x.Equals(this.native))
                            .Subscribe(x =>
                            {
                                x.SetPin(new byte[] { 0x0, 0x0, 0x0, 0x0 });
                                x.SetPairingConfirmation(true);
                            });
                    }
                    statusOb = BluetoothObservables
                        .WhenBondStatusChanged()
                        .Where(x => x.Equals(this.native) && x.BondState != Bond.Bonding)
                        .Subscribe(x => ob.Respond(x.BondState == Bond.Bonded));
                }
                return () =>
                {
                    requestOb?.Dispose();
                    statusOb?.Dispose();
                };
            });
        }
        /*
byte[] pinBytes = convertPinToBytes("0000");
try {
Log.d(TAG, "Try to set the PIN");
Method m = device.getClass().getMethod("setPin", byte[].class);
m.invoke(device, pinBytes);
Log.d(TAG, "Success to add the PIN.");
try {
device.getClass().getMethod("setPairingConfirmation", boolean.class).invoke(device, true);
Log.d(TAG, "Success to setPairingConfirmation.");
} catch (Exception e) {
// TODO Auto-generated catch block
Log.e(TAG, e.getMessage());
e.printStackTrace();
}
} catch (Exception e) {
Log.e(TAG, e.getMessage());
e.printStackTrace();
}
         *
byte[] pin = (byte[]) BluetoothDevice.class.getMethod("convertPinToBytes", String.class).invoke(BluetoothDevice.class, "1234");
Method m = mBluetoothDevice.getClass().getMethod("setPin", byte[].class);
m.invoke(mBluetoothDevice, pin);
mBluetoothDevice.getClass().getMethod("setPairingConfirmation", boolean.class).invoke(mBluetoothDevice, true);
                                         */

        public override PairingStatus PairingStatus
        {
            get
            {
                switch (this.native.BondState)
                {
                    case Bond.Bonded:
                        return PairingStatus.Paired;

                    default:
                    case Bond.None:
                        return PairingStatus.NotPaired;
                }
            }
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


        public override int GetHashCode()
        {
            return this.native.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var other = obj as Device;
            if (other == null)
                return false;

            // TODO: native might not be ready
            //if (!this.native.Equals(other.native))
            //    return false;

            return true;
        }


        public override string ToString()
        {
            return this.Uuid.ToString();
        }
    }
}