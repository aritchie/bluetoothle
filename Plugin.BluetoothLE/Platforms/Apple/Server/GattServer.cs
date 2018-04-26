using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly CBPeripheralManager manager;
        public GattServer(CBPeripheralManager peripheralManager)
            => this.manager = peripheralManager;


        protected override void Dispose(bool disposing)
            => this.manager.RemoveAllServices();


        public override IGattService CreateService(Guid uuid, bool primary) => new GattService(this.manager, this, uuid, primary);

        protected override void AddNative(IGattService service)
        {
            //CBPeripheralManager.AuthorizationStatus;
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

            Debug.WriteLine($"STATE: {this.manager.State} - AUTH: {CBPeripheralManager.AuthorizationStatus}");
            this.manager.ServiceAdded += (sender, args) =>
            {
                if (args.Error != null)
                    Debug.WriteLine($"ERROR: {args.Error.LocalizedDescription}");
            };
            this.manager.AddService(nativeService);
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