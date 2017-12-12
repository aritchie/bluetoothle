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
        GattServiceProviderResult server;


        IObservable<bool> runOb;
        public override IObservable<bool> WhenRunningChanged() => null;


        public override async Task Start()
        {
            foreach (var service in this.Services.OfType<IUwpGattService>())
            {
                await service.Init();
            }
            //base.Start();
        }


        public override void Stop()
        {
        }


        protected override IGattService CreateNative(Guid uuid, bool primary) => new UwpGattService(this, uuid, primary);
        protected override void ClearNative() => this.StopAll();
        protected override void RemoveNative(IGattService service) => ((IUwpGattService)service).Stop();
        protected virtual void StopAll()
        {
            foreach (var service in this.Services.OfType<IUwpGattService>())
                service.Stop();
        }
    }
}
