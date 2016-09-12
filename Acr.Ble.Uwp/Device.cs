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
        readonly GattDeviceService native;


        public Device(GattDeviceService native)
        {
            this.native = native;
        }


        public string Name => this.native.Device.Name;
        public Guid Uuid => this.native.Uuid;


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
            });
        }


        public IObservable<int> WhenRssiUpdated(TimeSpan? frequency = null)
        {
            throw new NotImplementedException();
        }


        public void Disconnect()
        {
        }



        public ConnectionStatus Status
        {
            get
            {
                switch (this.native.Device.ConnectionStatus)
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
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Status)
                );
                this.native.Device.ConnectionStatusChanged += handler;
                return () => this.native.Device.ConnectionStatusChanged -= handler;
            });
        }


        IObservable<IGattService> serviceOb;
        public IObservable<IGattService> WhenServiceDiscovered()
        {
            return Observable.Create<IGattService>(ob =>
            {

                foreach (var service in this.native.Device.GattServices)
                {

                }
                return Disposable.Empty;
            });
        }


        public IObservable<string> WhenNameUpdated()
        {
            return Observable.Create<string>(ob =>
            {
                ob.OnNext(this.Name);
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                this.native.Device.NameChanged += handler;
                return () => this.native.Device.NameChanged -= handler;;
            });
        }
    }
}