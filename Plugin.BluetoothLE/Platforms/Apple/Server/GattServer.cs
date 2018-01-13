using System;
using System.Linq;
using CoreBluetooth;


namespace Plugin.BluetoothLE.Server
{
    public class GattServer : AbstractGattServer
    {
        readonly CBPeripheralManager manager;


        public GattServer()
        {
            this.manager = new CBPeripheralManager();
        }


        protected override void Dispose(bool disposing)
        {
            this.manager.RemoveAllServices();
            this.manager.Dispose();
        }


        public override IGattService CreateService(Guid uuid, bool primary) => new GattService(this.manager, this, uuid, primary);

        protected override void AddNative(IGattService service)
        {
            var nativeService = ((IAppleGattService) service).Native;
            nativeService.Characteristics = service
                .Characteristics
                .Cast<IAppleGattCharacteristic>()
                .Select(x =>
                {
                    x.Native.Descriptors = x
                        .Descriptors
                        .Cast<IAppleGattDescriptor>()
                        .Select(y => y.Native)
                        .ToArray();

                    return x.Native;
                })
                .ToArray();

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