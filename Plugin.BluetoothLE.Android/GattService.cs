using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Bluetooth;
using Plugin.BluetoothLE.Internals;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly DeviceContext context;
        readonly BluetoothGattService native;


        public GattService(IDevice device,
                           DeviceContext context,
                           BluetoothGattService native) : base(device,
                                                               native.Uuid.ToGuid(),
                                                               native.Type == GattServiceType.Primary)
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


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            //=> this.context.Lock(Observable.Create<IGattCharacteristic>(ob =>
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var cids = characteristicIds.Select(x => x.ToUuid()).ToArray();
                foreach (var cid in cids)
                {
                    var cs = this.native.GetCharacteristic(cid);
                    var characteristic = new GattCharacteristic(this, this.context, cs);
                    ob.OnNext(characteristic);
                }
                ob.OnCompleted();

                return Disposable.Empty;
            });
            //}));


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
        public override string ToString() => $"Device {this.Uuid}";
    }
}