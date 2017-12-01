using System;
using System.Reactive.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly BluetoothLeDevice native;
        BluetoothGattClient gatt;


        public Device(BluetoothLeDevice native)
        {
            this.native = native;
        }


        public override ConnectionStatus Status
        {
            get
            {
                if (this.gatt == null)
                    return ConnectionStatus.Disconnected;

                return ConnectionStatus.Connected;
            }
        }


        public override IObservable<object> Connect(GattConnectionConfig config) => Observable.Create<object>(ob =>
        {

            this.native.GattConnect(config.AutoConnect);
            return () => { };
        });


        public override void CancelConnection()
        {
            this.gatt?.DestroyClient();
            this.gatt = null;
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan) => Observable.Create<int>(ob =>
        {
            return () => { };
        });


        public override IObservable<ConnectionStatus> WhenStatusChanged() => Observable.Create<ConnectionStatus>(ob =>
        {
            var handler = new EventHandler<GattConnectionStateChangedEventArgs>((sender, args) =>
            {
            });
            this.native.GattConnectionStateChanged += handler;
            return () => this.native.GattConnectionStateChanged -= handler;
        })
        .StartWith(this.Status);


        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            this.serviceOb = this.serviceOb ?? Observable.Create<IGattService>(ob =>
            {
                var services = this.gatt.GetServices();

                return () => { };
            })
            .ReplayWithReset(this
                .WhenStatusChanged()
                .Where(x => x == ConnectionStatus.Disconnected)
            )
            .RefCount();

            return this.serviceOb;
        }


        public override DeviceFeatures Features { get; }
        public override object NativeDevice => this.native;
    }
}
