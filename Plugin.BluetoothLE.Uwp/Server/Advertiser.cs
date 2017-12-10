using System;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;


namespace Plugin.BluetoothLE.Server
{
    public class Advertiser : AbstractAdvertiser
    {
        readonly BluetoothLEAdvertisementPublisher publisher;

        //this.publisher.Status == BluetoothLEAdvertisementPublisherStatus.Started;

        public Advertiser()
        {
            this.publisher = new BluetoothLEAdvertisementPublisher();
        }


        public override void Start(AdvertisementData adData)
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
                this.publisher.Advertisement.ServiceUuids.Add(serviceUuid);

            this.publisher.Start();
            base.Start(adData);
        }
    }
}
