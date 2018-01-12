using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        protected override void ClearNative() => this.Dispose();
        public override IGattService CreateService(Guid uuid, bool primary) => new UwpGattService(this, uuid, primary);


        protected override void AddNative(IGattService service)
        {
            var native = service as IUwpGattService;
            native.Init().Wait();
        }


        protected override void RemoveNative(IGattService service) => ((IUwpGattService)service).Stop();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var service in this.Services.OfType<IUwpGattService>())
                service.Stop();
        }
    }
}
