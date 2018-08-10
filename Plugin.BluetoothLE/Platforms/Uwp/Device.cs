using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly DeviceContext context;


        public Device(AdapterContext adapterContext, BluetoothLEDevice native)
        {
            this.context = new DeviceContext(adapterContext, this, native);
            this.Name = native.Name;
            this.Uuid = native.GetDeviceId();
        }


        public override object NativeDevice => this.context.NativeDevice;
        public override DeviceFeatures Features => DeviceFeatures.PairingRequests | DeviceFeatures.ReliableTransactions;
        public override IGattReliableWriteTransaction BeginReliableWriteTransaction() => new GattReliableWriteTransaction();

        public override void Connect(ConnectionConfig config) => this.context.Connect();
        public override void CancelConnection() => this.context.Disconnect();
        public override ConnectionStatus Status => this.context.Status;
        public override IObservable<ConnectionStatus> WhenStatusChanged() => this.context.WhenStatusChanged();


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => Observable.FromAsync(async ct =>
        {
            var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Cached);
            if (result.Status != GattCommunicationStatus.Success)
                throw new ArgumentException("Could not find GATT service - " + result.Status);

            var wrap = new GattService(this.context, result.Services.First());
            return wrap;
        });


        public override IObservable<IGattService> DiscoverServices() => Observable.Create<IGattService>(async ob =>
        {
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
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                var sub = this.WhenConnected().Subscribe(_ =>
                    this.context.NativeDevice.NameChanged += handler
                );
                return () =>
                {
                    sub?.Dispose();
                    if (this.context.NativeDevice != null)
                        this.context.NativeDevice.NameChanged -= handler;
                };
            })
            .StartWith(this.Name)
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public override PairingStatus PairingStatus => this.context.NativeDevice.DeviceInformation.Pairing.IsPaired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public override IObservable<bool> PairingRequest(string pin = null) => Observable.FromAsync(async token =>
        {
            var result = await this.context.NativeDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
            var state = result.Status == DevicePairingResultStatus.Paired;
            return state;
        });
    }
}