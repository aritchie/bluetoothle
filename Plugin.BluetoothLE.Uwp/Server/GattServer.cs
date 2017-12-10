using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        GattServiceProviderResult server;


        public override IObservable<bool> WhenRunningChanged() => null;
        public override bool IsRunning { get; } = false;


        public override async Task Start()
        {
            foreach (var service in this.Services.OfType<IUwpGattService>())
            {
                await service.Init();
            }
        }


        public override void Stop()
        {
            //this.server.ServiceProvider.
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