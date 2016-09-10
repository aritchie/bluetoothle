using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Acr.Ble
{
    public class Device : IDevice
    {
        readonly ulong bluetoothAddress;
        BluetoothLEDevice connection;


        public Device(ulong bluetoothAddress, string localName)
        {
            this.bluetoothAddress = bluetoothAddress;
            this.Name = localName;
        }


        public string Name { get; private set; }
        public Guid Uuid { get; } = Guid.Empty;


        public IObservable<ConnectionStatus> PersistentConnect()
        {
            throw new NotImplementedException();
        }


        public IObservable<object> Connect()
        {
             //var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
                //await BluetoothLEDevice.FromIdAsync(devices[0].Id)
                //DeviceInformation.FindAllAsync(GattDeviceService.)
            return Observable.Create<object>(async ob =>
            {
                this.connection = await BluetoothLEDevice.FromBluetoothAddressAsync(this.bluetoothAddress);

            });
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            throw new NotImplementedException();
        }


        public void Disconnect()
        {
            this.connection.Dispose();
            this.connection = null;
        }



        public ConnectionStatus Status
        {
            get
            {
                if (this.connection == null)
                    return ConnectionStatus.Disconnected;

                switch (this.connection.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionStatus.Connected;

                    default:
                        return ConnectionStatus.Disconnected;
                }
            }
        }


        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            return Observable.Create<ConnectionStatus>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEDevice, object>((sender, args) => ob.OnNext(this.Status));
                //this.native.ConnectionStatusChanged += handler;
                //return () => this.native.ConnectionStatusChanged -= handler;
                return () => { };
            });
        }


        public IObservable<IGattService> WhenServiceDiscovered()
        {
            return Observable.Create<IGattService>(ob =>
            {
                foreach (var service in this.connection.GattServices)
                {

                }
                return Disposable.Empty;
            });
        }


        public IObservable<string> WhenNameUpdated()
        {
            return Observable.Create<string>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEDevice, object>((sender, args) => ob.OnNext(this.Name));
                //this.native.NameChanged += handler;
                //return () => this.native.NameChanged -= handler;
                return () => { };
            });
        }
    }
}