using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly CBPeripheralManager manager;
        public GattServer() => this.manager = new CBPeripheralManager();


        protected override void Dispose(bool disposing)
        {
            this.manager.RemoveAllServices();
            this.manager.Dispose();
        }


        public override IGattService CreateService(Guid uuid, bool primary) => new GattService(this.manager, this, uuid, primary);

        protected override void AddNative(IGattService service)
        {
            var nativeService = ((IAppleGattService) service).Native;
            var chlist = new List<CBCharacteristic>();

            foreach (var ch in service.Characteristics.OfType<IAppleGattCharacteristic>())
            {
                chlist.Add(ch.Native);

                var dlist = new List<CBDescriptor>();
                foreach (var desc in ch.Descriptors.OfType<IAppleGattDescriptor>())
                    dlist.Add(desc.Native);

                ch.Native.Descriptors = dlist.ToArray();
            }
            nativeService.Characteristics = chlist.ToArray();
            this.manager.AddService(nativeService);
            if (!this.manager.Advertising)
                this.manager.StartAdvertising(new StartAdvertisingOptions());
        }


        protected override void RemoveNative(IGattService service)
        {
            var native = ((IAppleGattService)service).Native;
            this.manager.RemoveService(native);
        }


        protected override void ClearNative()
        {
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