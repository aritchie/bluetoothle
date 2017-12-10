using System;
using System.Linq;
using CoreBluetooth;

namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly CBPeripheralManager manager = new CBPeripheralManager();
        //public override bool IsRunning => this.manager.Advertising;

        public override void Start(AdvertisementData adData)
        {
            this.manager.StartAdvertising(new StartAdvertisingOptions
            {
                LocalName = adData.LocalName,
                ServicesUUID = adData
                    .ServiceUuids
                    .Select(x => CBUUID.FromString(x.ToString()))
                    .ToArray()
            });            
            base.Start(adData);
        }


        public override void Stop()
        {
            base.Stop();
        }
    }
}
/*
            this.runningOb = this.runningOb ?? Observable.Create<bool>(ob =>
            {
                var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
                {
                    if (args.Error == null)
                    {
                        ob.OnNext(true);
                    }
                    else
                    {
                        ob.OnError(new ArgumentException(args.Error.LocalizedDescription));
                    }
                });
                this.manager.AdvertisingStarted += handler;

                var sub = this.runningSubj
                    .AsObservable()
                    .Subscribe(ob.OnNext);

                return () =>
                {
                    this.manager.AdvertisingStarted -= handler;
                    sub.Dispose();
                };
            })
            .Publish()
            .RefCount();

            return this.runningOb;
*/