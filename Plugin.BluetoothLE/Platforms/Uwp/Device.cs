using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Acr.Reactive;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly DeviceContext context;
        readonly ulong bluetoothAddress;


        public Device(AdapterContext adapterContext, BluetoothLEDevice native)
        {
            this.context = new DeviceContext(adapterContext, this, native);
            this.bluetoothAddress = native.BluetoothAddress;
            this.Uuid = native.GetDeviceId();
        }


        public override string Name => this.context.NativeDevice.Name;
        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.PairingRequests | DeviceFeatures.ReliableTransactions;
        public override IGattReliableWriteTransaction BeginReliableWriteTransaction() => new GattReliableWriteTransaction();


        public override async void Connect(ConnectionConfig config)
        {
            if (this.NativeDevice != null)
                return;

            this.context.NativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(this.bluetoothAddress);
            // TODO: switch to connecting
        }


        public override async void CancelConnection() => await this.context.Disconnect();


        public override ConnectionStatus Status
        {
            get
            {
                if (this.context.NativeDevice == null)
                    return ConnectionStatus.Disconnected;

                switch (this.context.NativeDevice.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => Observable.FromAsync(async ct =>
            {
                var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Cached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("Could not find GATT service - " + result.Status);

                var wrap = new GattService(this.context, result.Services.First());
                return wrap;
            });


        IObservable<ConnectionStatus> statusOb;
        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                this.context.NativeDevice.ConnectionStatusChanged += handler;

                return () => this.context.NativeDevice.ConnectionStatusChanged -= handler;
            })
            .Replay(1)
            .RefCount();

            return this.statusOb;
        }


        public override IObservable<IGattService> DiscoverServices() => Observable.Create<IGattService>(async ob =>
        {
            //var handler = new TypedEventHandler<BluetoothLEDevice, object>((sender, args) =>
            //{
            //    //if (this.native.Equals(sender))
            //});
            //this.context.NativeDevice.GattServicesChanged += handler;

            var result = await this.context.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            foreach (var nservice in result.Services)
            {
                var service = new GattService(this.context, nservice);
                ob.OnNext(service);
            }
            ob.OnCompleted();

            return Disposable.Empty;
        });


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                this.context.NativeDevice.NameChanged += handler;

                return () => this.context.NativeDevice.NameChanged -= handler;
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public override PairingStatus PairingStatus => this.context.NativeDevice.DeviceInformation.Pairing.IsPaired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public override IObservable<bool> PairingRequest(string pin = null)
            => Observable.FromAsync(async token =>
            {
                var result = await this.context.NativeDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                var state = result.Status == DevicePairingResultStatus.Paired;
                return state;
            });
    }
}