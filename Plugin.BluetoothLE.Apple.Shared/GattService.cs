using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly CBService native;


        public GattService(IDevice device, CBService native) : base(device, native.UUID.ToGuid(), native.Primary)
        {
            this.native = native;
        }


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = new Dictionary<Guid, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (!this.Equals(args.Service))
                        return;

                    foreach (var nch in this.native.Characteristics)
                    {
                        var ch = new GattCharacteristic(this, nch);
                        if (!characteristics.ContainsKey(ch.Uuid))
                        {
                            characteristics.Add(ch.Uuid, ch);
                            ob.OnNext(ch);
                        }
                    }
                });
                this.native.Peripheral.DiscoveredCharacteristic += handler;
                this.native.Peripheral.DiscoverCharacteristics(this.native);

                return () => this.native.Peripheral.DiscoveredCharacteristic -= handler;
            })
            .Replay()
            .RefCount();

            return this.characteristicOb;
        }


        bool Equals(CBService service)
        {
            if (!this.native.UUID.Equals(service.UUID))
                return false;

			if (!this.native.Peripheral.Identifier.Equals(service.Peripheral.Identifier))
                return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            var other = obj as GattService;
            if (other == null)
                return false;

			if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}
