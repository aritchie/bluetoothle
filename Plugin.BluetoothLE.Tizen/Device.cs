using System;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class Device : AbstractDevice
    {
        readonly BluetoothLeDevice native;


        public Device(BluetoothLeDevice native)
        {
            this.native = native;
        }


        public override ConnectionStatus Status { get; }
        public override IObservable<object> Connect(GattConnectionConfig config)
        {
            throw new NotImplementedException();
        }


        public override void CancelConnection()
        {
            throw new NotImplementedException();
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan)
        {
            throw new NotImplementedException();
        }


        public override IObservable<ConnectionStatus> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IGattService> WhenServiceDiscovered()
        {
            throw new NotImplementedException();
        }


        public override DeviceFeatures Features { get; }
        public override object NativeDevice { get; }
    }
}
