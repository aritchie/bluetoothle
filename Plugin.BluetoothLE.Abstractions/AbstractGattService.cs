using System;


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
        public virtual string Description => Dictionaries.GetServiceDescription(this.Uuid);
        public abstract IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
    }
}
