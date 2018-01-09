using System;
using System.Linq;
using Android.App;
using Android.Bluetooth;
using Android.Content;
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


        //public override Task Start()
        //{

        //    this.context.Server = this.server;

            //foreach (var service in this.Services.OfType<IDroidGattService>())
            //{
            //    var nativeService = service.CreateNative();
            //    if (!this.context.Server.AddService(service.Native))
            //        throw new ArgumentException($"Could not add service {service.Uuid} to server");

            //    foreach (var characteristic in service.Characteristics.OfType<IDroidGattCharacteristic>())
            //    {
            //        if (!service.Native.AddCharacteristic(characteristic.Native))
            //            throw new ArgumentException($"Could not add characteristic '{characteristic.Uuid}' to service '{service.Uuid}'");

            //        foreach (var descriptor in characteristic.Descriptors.OfType<IDroidGattDescriptor>())
            //        {
            //            if (!characteristic.Native.AddDescriptor(descriptor.Native))
            //                throw new ArgumentException($"Could not add descriptor '{descriptor.Uuid}' to characteristic '{characteristic.Uuid}'");
            //        }
            //    }
            //    this.server.AddService(service.Native);
            //}

            //this.runningSubj.OnNext(true);
            //this.isRunning = true;
        //    return Task.CompletedTask;
        //}


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.server?.Close();
            //this.server = null;
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


        protected override void ClearNative() => this.server?.ClearServices();
    }
}