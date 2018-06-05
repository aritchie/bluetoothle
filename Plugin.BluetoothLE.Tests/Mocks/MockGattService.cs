using System;
namespace Plugin.BluetoothLE.Tests.Mocks
{
    public class MockGattService : IGattService
    {

        public IDevice Device { get; set; }

        public Guid Uuid => this.Device?.Uuid ?? Guid.NewGuid();

        public string Description => throw new NotImplementedException();

        public IObservable<IGattCharacteristic> DiscoverCharacteristics()
        {
            throw new NotImplementedException();
        }

        public IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
        {
            throw new NotImplementedException();
        }
    }
}
