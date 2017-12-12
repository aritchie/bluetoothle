using Plugin.BluetoothLE.Server;
using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;


namespace Plugin.BluetoothLE
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly BluetoothLEAdvertisementPublisher publisher = new BluetoothLEAdvertisementPublisher();


        public override async Task Start(AdvertisementData adData)
        {
            this.publisher.Advertisement.Flags = BluetoothLEAdvertisementFlags.ClassicNotSupported;
            this.publisher.Advertisement.ManufacturerData.Clear();
            this.publisher.Advertisement.ServiceUuids.Clear();

            if (adData.ManufacturerData != null)
            {
                using (var writer = new DataWriter())
                {
                    writer.WriteBytes(adData.ManufacturerData.Data);
                    var md = new BluetoothLEManufacturerData(adData.ManufacturerData.CompanyId, writer.DetachBuffer());
                    this.publisher.Advertisement.ManufacturerData.Add(md);
                }
            }

            foreach (var serviceUuid in adData.ServiceUuids)
            {
                this.publisher.Advertisement.ServiceUuids.Add(serviceUuid);
            }

            this.publisher.Start();
            await base.Start();
        }


        public override void Stop()
        {
            this.publisher.Stop();
        }
        //this.publisher.Status == BluetoothLEAdvertisementPublisherStatus.Started;
    }
}
/*
         public override IObservable<bool> WhenRunningChanged()
        {
            this.runOb = this.runOb ?? Observable.Create<bool>(ob =>
            {
                ob.OnNext(this.IsRunning);
                var handler = new TypedEventHandler<BluetoothLEAdvertisementPublisher, BluetoothLEAdvertisementPublisherStatusChangedEventArgs>(
                    (sender, args) => ob.OnNext(this.IsRunning)
                );
                this.publisher.StatusChanged += handler;
                return () => this.publisher.StatusChanged -= handler;
            })
            .Repeat(1);

            return this.runOb;
        }

     */
