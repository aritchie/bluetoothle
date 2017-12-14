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
        readonly GattContext context;
        readonly Subject<bool> runningSubj;
        BluetoothGattServer server;


        public GattServer()
        {
            this.manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            this.context = new GattContext();
            this.runningSubj = new Subject<bool>();
        }


        bool isRunning = false;
        public override bool IsRunning => this.isRunning;


        public override IObservable<bool> WhenRunningChanged() => this.runningSubj;


        public override Task Start()
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

            this.runningSubj.OnNext(true);
            this.isRunning = true;
            return Task.CompletedTask;
        }


        public override void Stop()
        {
            if (!this.isRunning)
                return;

            this.isRunning = false;

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


        protected override void ClearNative() => this.server?.ClearServices();
    }
}