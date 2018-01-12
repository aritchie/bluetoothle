using System;
using System.Linq;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Java.Util;
using Plugin.BluetoothLE.Server.Internals;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly BluetoothManager manager;
        readonly GattContext context;
        readonly BluetoothGattServer server;


        public GattServer()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.server = this.manager.OpenGattServer(Application.Context, this.context.Callbacks);
            this.context = new GattContext(this.server);
        }


        public override IGattService CreateService(Guid uuid, bool primary) => new GattService(this.context, this, uuid, primary);


        protected override void AddNative(IGattService service)
        {
            var native = service as IDroidGattService;
            if (native == null)
                throw new ArgumentException("Service does not inherit IDroidGattService");

            if (native.Characteristics.Count == 0)
                throw new ArgumentException("No characteristics added to service");

            this.server.AddService(native.Native);
        }


        protected override void RemoveNative(IGattService service)
        {
            var nuuid = UUID.FromString(service.Uuid.ToString());
            var native = this.server.Services.FirstOrDefault(x => x.Uuid.Equals(nuuid));
            if (native != null)
                this.server.RemoveService(native);
        }


        protected override void ClearNative() => this.server?.ClearServices();


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.server?.Close();
        }
    }
}