using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly CBPeripheralManager manager = new CBPeripheralManager();
        readonly IList<IGattService> services = new List<IGattService>();
        readonly Subject<bool> runningSubj = new Subject<bool>();


        public override IObservable<bool> WhenRunningChanged() => this.runningSubj;
        public override bool IsRunning { get; }


        public override Task Start()
        {
            //if (CBPeripheralManager.AuthorizationStatus != CBPeripheralManagerAuthorizationStatus.Authorized)
            //    throw new ArgumentException("Permission Denied - " + CBPeripheralManager.AuthorizationStatus);

            //if (this.manager.State != CBPeripheralManagerState.PoweredOn)
                //throw new ArgumentException("Invalid State - " + this.manager.State);

            this.services
                .Cast<IIosGattService>()
                .Select(x =>
                {
                    x.Native.Characteristics = x
                        .Characteristics
                        .OfType<IIosGattCharacteristic>()
                        .Select(y =>
                        {
                            y.Native.Descriptors = y
                                .Descriptors
                                .OfType<IIosGattDescriptor>()
                                .Select(z => z.Native)
                                .ToArray();
                            return y.Native;
                        })
                        .ToArray();

                    return x.Native;
                })
                .ToList()
                .ForEach(this.manager.AddService);

            //this.IsRunning = true;
            return Task.CompletedTask;
        }


        public override void Stop()
        {
            this.manager.RemoveAllServices();
            this.manager.StopAdvertising();
            //this.IsRunning = false;
            this.runningSubj.OnNext(false);
        }


        protected override IGattService CreateNative(Guid uuid, bool primary)
        {
            var service = new GattService(this.manager, this, uuid, primary);
            this.services.Add(service);
            //this.context?.Manager.AddService(service.Native); // TODO: build the service out?
            return service;
        }


        protected override void RemoveNative(IGattService service)
        {
            if (this.services.Remove(service))
            {
                var native = ((IIosGattService)service).Native;
                this.manager.RemoveService(native);
            }
        }


        protected override void ClearNative()
        {
            this.services.Clear();
            this.manager.RemoveAllServices();
        }
    }
}