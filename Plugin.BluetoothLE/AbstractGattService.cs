using System;
using System.Linq;
using System.Reactive.Linq;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractGattService : IGattService
    {
        protected AbstractGattService(IDevice device, Guid uuid, bool primary)
        {
            this.Device = device;
            this.Uuid = uuid;
            this.IsPrimary = primary;
        }


        public IDevice Device { get; }
        public Guid Uuid { get; }
        public bool IsPrimary { get; }

        public abstract IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();

        public virtual string Description => Dictionaries.GetServiceDescription(this.Uuid);
        public virtual IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => this.WhenCharacteristicDiscovered().Where(x => characteristicIds.Any(y => y == x.Uuid));
    }
}
