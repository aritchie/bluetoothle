using System;
using System.Diagnostics;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Acr.Ble
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
            this.characteristicOb = this.characteristicOb ?? Observable
                .Create<IGattCharacteristic>(ob =>
                {
                    Debug.WriteLine($"Characteristic Discovery Started for Service {this.Uuid}");
                    var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                    {
                        if (!args.Service.Equals(native))
                            return;

                        foreach (var nch in native.Characteristics)
                        {
                            var ch = new GattCharacteristic(this, nch);
                            ob.OnNext(ch);
                        }
                    });
                    this.native.Peripheral.DiscoveredCharacteristic += handler;
                    this.native.Peripheral.DiscoverCharacteristics(native);

                    return () => 
                    {
                        Debug.WriteLine($"Characteristic Discovery Ending for Service {this.Uuid}");
                        this.native.Peripheral.DiscoveredCharacteristic -= handler;
                    };
                })
                .Replay()
                .RefCount();
            
            return this.characteristicOb;
        }
    }
}
