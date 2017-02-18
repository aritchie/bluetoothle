using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Bluetooth;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly GattContext context;
        readonly BluetoothGattService native;


        public GattService(IDevice device, GattContext context, BluetoothGattService native)
                : base(device, native.Uuid.ToGuid(), native.Type == GattServiceType.Primary)
        {
            this.context = context;
            this.native = native;
        }


        IObservable<IGattCharacteristic> characteristicOb;
        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered()
        {
            this.characteristicOb = this.characteristicOb ?? Observable.Create<IGattCharacteristic>(ob =>
            {
                foreach (var nch in this.native.Characteristics)
                {
                    var wrap = new GattCharacteristic(this, this.context, nch);
                    ob.OnNext(wrap);
                }
                return Disposable.Empty;
            })
            .Replay()
            .RefCount();

            return this.characteristicOb;
        }


        public override int GetHashCode()
        {
            return this.native.GetHashCode();
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattService;
            if (other == null)
                return false;

            if (!this.native.Equals(other.native))
                return false;

            return true;
        }


        public override string ToString()
        {
            return this.Uuid.ToString();
        }
    }
}