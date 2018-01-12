using System;
using System.Collections.Generic;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly IList<IGattService> services;
        readonly CBPeripheralManager manager;


        public GattServer()
        {
            this.services = new List<IGattService>();
            this.manager = new CBPeripheralManager();
        }


        protected override void Dispose(bool disposing)
        {
            this.manager.RemoveAllServices();
            this.manager.Dispose();
        }


        public override IGattService CreateService(Guid uuid, bool primary) => new GattService(this.manager, this, uuid, primary);
        protected override void AddNative(IGattService service) => this.manager.AddService(((IAppleGattService)service).Native);


        protected override void RemoveNative(IGattService service)
        {
            if (this.services.Remove(service))
            {
                var native = ((IAppleGattService)service).Native;
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
//CBPeripheralManager.AuthorizationStatus
//if (this.manager.Advertising)
//    return Task.CompletedTask;

//if (CBPeripheralManager.AuthorizationStatus != CBPeripheralManagerAuthorizationStatus.Authorized)
//    throw new ArgumentException("Permission Denied - " + CBPeripheralManager.AuthorizationStatus);

//if (this.manager.State != CBPeripheralManagerState.PoweredOn)
//    throw new ArgumentException("Invalid State - " + this.manager.State);