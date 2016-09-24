using System;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Acr.Ble
{
    public class Device : IDevice
    {
        readonly AdvertisementData adData;
        GattDeviceService native;

        public Device(AdvertisementData adData)
        {
            this.adData = adData;
        }


        public string Name => this.adData.LocalName; // need complete name?
        public Guid Uuid => Guid.Empty; //this.native.Uuid;


        public IObservable<ConnectionStatus> CreateConnection()
        {
            return Observable.Create<ConnectionStatus>(async ob =>
            {
                var status = this
                    .WhenStatusChanged()
                    .Subscribe(ob.OnNext);
                await this.Connect();

                return status;
            });
        }


        public IObservable<object> Connect()
        {
            return Observable.Create<object>(async ob =>
            {
                var all = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess), null);

                //var all = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(this.adData.BluetoothAddress));
                //var devInfo = all.SingleOrDefault();

                //var ble = await BluetoothLEDevice.FromBluetoothAddressAsync(this.adData.BluetoothAddress);
                //var p = ble.DeviceInformation.Pairing;
                //if (p.CanPair && p.IsPaired)
                //    await p.PairAsync();

                //this.native = await BluetoothLEDevice.FromIdAsync(ble.DeviceId);

                return () => { };
            });
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            return Observable.Empty<int>();
        }


        public void Disconnect()
        {
            if (this.Status != ConnectionStatus.Connected)
                return;

            this.native?.Dispose();
            this.native = null;
        }


        public ConnectionStatus Status
        {
            get
            {
                if (this.native == null)
                    return ConnectionStatus.Disconnected;

                switch (this.native.Device.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        IObservable<ConnectionStatus> statusOb;
        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            this.statusOb = this.statusOb ?? Observable.Create<ConnectionStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                // TODO: when device ready
                //this.native.Device.ConnectionStatusChanged += handler;
                //return () => this.native.Device.ConnectionStatusChanged -= handler;
                return () => { };
            })
            .Publish()
            .RefCount();

            return this.statusOb;
        }


        IObservable<IGattService> serviceOb;
        public IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
                this
                    .WhenStatusChanged()
                    .Where(x => x == ConnectionStatus.Connected)
                    .Subscribe(x =>
                    {
                        foreach (var nservice in this.native.Device.GattServices)
                        {
                            var service = new GattService(nservice, this);
                            ob.OnNext(service);
                        }
                    })
            )
            .ReplayWithReset(this.WhenStatusChanged()
                .Skip(1)
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.serviceOb;
        }


        IObservable<string> nameOb;
        public IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);

                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                //this.native.Device.NameChanged += handler;
                //return () => this.native.Device.NameChanged -= handler;
                return () => { };
            })
            .Publish()
            .RefCount();

            return this.nameOb;
        }
    }
}