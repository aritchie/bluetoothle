using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly BluetoothLEAdvertisementPublisher publisher;
        GattServiceProviderResult server;


        public GattServer()
        {
            this.publisher = new BluetoothLEAdvertisementPublisher();
        }


        IObservable<bool> runOb;
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


        public override bool IsRunning => this.publisher.Status == BluetoothLEAdvertisementPublisherStatus.Started;


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
            await this.StartGatt();
            this.publisher.Start();
        }


        public override void Stop()
        {
            this.publisher.Stop();
        }


        protected virtual async Task StartGatt()
        {
            foreach (var service in this.Services.OfType<IUwpGattService>())
            {
                await service.Init();
            }
        }

        protected override IGattService CreateNative(Guid uuid, bool primary)
        {
            return new UwpGattService(this, uuid, primary);
        }


        protected override void ClearNative()
        {
            this.StopAll();
        }


        protected override void RemoveNative(IGattService service)
        {
            ((IUwpGattService)service).Stop();
        }


        protected virtual void StopAll()
        {
            foreach (var service in this.Services.OfType<IUwpGattService>())
                service.Stop();
        }
    }
}
/*
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Storage.Streams;


namespace Plugin.BeaconAds
{
    public class BeaconAdvertiser : IBeaconAdvertiser
    {
        readonly BluetoothLEAdvertisementPublisher publiser;
        readonly Lazy<Radio> radio;


        public BeaconAdvertiser()
        {
            this.publiser = new BluetoothLEAdvertisementPublisher();
            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public Status Status
        {
            get
            {
                if (this.radio.Value == null)
                    return Status.Unsupported;

                switch (this.radio.Value.State)
                {
                    case RadioState.Disabled:
                    case RadioState.Off:
                        return Status.PoweredOff;

                    case RadioState.Unknown:
                        return Status.Unknown;

                    default:
                        return Status.PoweredOn;
                }
            }
        }


        public Beacon AdvertisedBeacon { get; private set; }


        public void Start(Beacon beacon)
        {


            var writer = new DataWriter();
            writer.WriteBytes(beacon.ToIBeaconPacket(10));
            var md = new BluetoothLEManufacturerData(76, writer.DetachBuffer());
            this.publiser.Advertisement.ManufacturerData.Add(md);
            this.publiser.Start();

            //var trigger = new BluetoothLEAdvertisementPublisherTrigger();
            //trigger.Advertisement.ManufacturerData.Add(md);
            this.AdvertisedBeacon = beacon;
        }


        public void Stop()
        {
            this.publiser.Stop();
        }
    }
}

     */
