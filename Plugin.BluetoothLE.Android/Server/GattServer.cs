using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Plugin.BluetoothLE.Server.Internals;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly BluetoothManager manager;
        readonly AdvertisementCallbacks adCallbacks;
        readonly GattContext context;
        readonly Subject<bool> runningSubj;
        BluetoothGattServer server;


        public GattServer()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.adCallbacks = new AdvertisementCallbacks();
            this.context = new GattContext();
            this.runningSubj = new Subject<bool>();
        }


        bool isRunning = false;
        public override bool IsRunning => this.isRunning;


        IObservable<bool> runningOb;
        public override IObservable<bool> WhenRunningChanged()
        {
            return this.runningSubj;
            //this.runningOb = this.runningOb ?? Observable.Create<bool>(ob =>
            //{
            //    this.adCallbacks.Failed = ob.OnError;
            //    this.adCallbacks.Started = () => ob.OnNext(true);
            //    var sub = this.runningSubj
            //        .AsObservable()
            //        .Subscribe(x => ob.OnNext);

            //    return () =>
            //    {
            //        sub.Dispose();
            //        this.adCallbacks.Failed = null;
            //        this.adCallbacks.Started = null;
            //    };
            //})
            //.Publish()
            //.RefCount();

            //return this.runningOb;
        }


        public override Task Start(AdvertisementData adData)
        {
            if (this.isRunning)
                return Task.CompletedTask;

            if (adData != null)
                this.StartAdvertising(adData);

            this.StartGatt();
            this.runningSubj.OnNext(true);
            this.isRunning = true;
            return Task.CompletedTask;
        }


        public override void Stop()
        {
            if (!this.isRunning)
                return;

            this.isRunning = false;
            this.manager.Adapter.BluetoothLeAdvertiser.StopAdvertising(this.adCallbacks);
            this.context.Server = null;
            this.server?.Close();
            this.server = null;
            this.runningSubj.OnNext(false);
        }


        protected override IGattService CreateNative(Guid uuid, bool primary)
        {
            var service  = new GattService(this.context, this, uuid, primary);
            this.server?.AddService(service.Native);
            return service;
        }


        protected override void RemoveNative(IGattService service)
        {
            var nuuid = Java.Util.UUID.FromString(service.Uuid.ToString());
            var native = this.server.Services.FirstOrDefault(x => x.Uuid.Equals(nuuid));
            if (native != null)
                this.server?.RemoveService(native);
        }


        protected override void ClearNative()
        {
            this.server?.ClearServices();
        }


        protected virtual void StartAdvertising(AdvertisementData adData)
        {
            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true);

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(true)
                .SetIncludeTxPowerLevel(true);

            if (adData.ManufacturerData != null)
                data.AddManufacturerData(adData.ManufacturerData.CompanyId, adData.ManufacturerData.Data);

            foreach (var serviceUuid in adData.ServiceUuids)
            {
                var uuid = ParcelUuid.FromString(serviceUuid.ToString());
                data.AddServiceUuid(uuid);
            }

            this.manager
                .Adapter
                .BluetoothLeAdvertiser
                .StartAdvertising(
                    settings.Build(),
                    data.Build(),
                    this.adCallbacks
                );
        }


        protected virtual void StartGatt()
        {
            this.server = this.manager.OpenGattServer(Application.Context, this.context.Callbacks);
            this.context.Server = this.server;

            foreach (var service in this.Services.OfType<IDroidGattService>())
            {
                if (!this.context.Server.AddService(service.Native))
                    throw new ArgumentException($"Could not add service {service.Uuid} to server");

                foreach (var characteristic in service.Characteristics.OfType<IDroidGattCharacteristic>())
                {
                    if (!service.Native.AddCharacteristic(characteristic.Native))
                        throw new ArgumentException($"Could not add characteristic '{characteristic.Uuid}' to service '{service.Uuid}'");

                    foreach (var descriptor in characteristic.Descriptors.OfType<IDroidGattDescriptor>())
                    {
                        if (!characteristic.Native.AddDescriptor(descriptor.Native))
                            throw new ArgumentException($"Could not add descriptor '{descriptor.Uuid}' to characteristic '{characteristic.Uuid}'");
                    }
                }
                this.server.AddService(service.Native);
            }
        }
    }
}