using System;
namespace Plugin.BluetoothLE.Tests.Mocks
{
    public class MockDevice : IDevice
    {
        public object NativeDevice => throw new NotImplementedException();

        public DeviceFeatures Features => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public Guid Uuid { get; set; }

        public int MtuSize { get; set; }

        public PairingStatus PairingStatus => throw new NotImplementedException();

        public ConnectionStatus Status => throw new NotImplementedException();

        public MockGattReliableWriteTransaction Transaction { get; set; }

        public IGattReliableWriteTransaction BeginReliableWriteTransaction()
        {
            return  this.Transaction ?? new MockGattReliableWriteTransaction(); 
        }

        public void CancelConnection()
        {
            throw new NotImplementedException();
        }

        public void Connect(ConnectionConfig config = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<IGattService> DiscoverServices()
        {
            throw new NotImplementedException();
        }

        public IObservable<IGattService> GetKnownService(Guid serviceUuid)
        {
            throw new NotImplementedException();
        }

        public IObservable<bool> PairingRequest(string pin = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<int> ReadRssi()
        {
            throw new NotImplementedException();
        }

        public IObservable<int> RequestMtu(int size)
        {
            throw new NotImplementedException();
        }

        public IObservable<BleException> WhenConnectionFailed()
        {
            throw new NotImplementedException();
        }

        public IObservable<int> WhenMtuChanged()
        {
            throw new NotImplementedException();
        }

        public IObservable<string> WhenNameUpdated()
        {
            throw new NotImplementedException();
        }

        public IObservable<ConnectionStatus> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}
