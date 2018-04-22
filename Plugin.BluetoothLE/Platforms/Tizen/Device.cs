using System;
using System.Reactive.Linq;
using Acr;
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


        public override void Connect()
        {
            if (this.gatt != null)
                return;

            this.gatt = this.native.GattConnect(true);
            // TODO: connecting
        }


        public override void CancelConnection()
        {
            this.gatt?.DestroyClient();
            this.gatt = null;
        }


        public override IObservable<ConnectionStatus> WhenStatusChanged() => Observable.Create<ConnectionStatus>(ob =>
        {
            var handler = new EventHandler<GattConnectionStateChangedEventArgs>((sender, args) =>
            {
                //args.IsConnected;
            });
            this.native.GattConnectionStateChanged += handler;
            return () => this.native.GattConnectionStateChanged -= handler;
        })
        .StartWith(this.Status);


        //public override IGattService GetKnownService(Guid serviceUuid)
        //{

        //}

        IObservable<IGattService> serviceOb;
        public override IObservable<IGattService> DiscoverServices()
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
