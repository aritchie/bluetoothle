using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public class UwpGattService : AbstractGattService, IUwpGattService
    {
        GattServiceProviderResult native;


        public UwpGattService(IGattServer server, Guid serviceUuid, bool primary) : base(server, serviceUuid, primary)
        {
        }


        protected override IGattCharacteristic CreateNative(Guid uuid, CharacteristicProperties properties, GattPermissions permissions)
            => new UwpGattCharacteristic(this, uuid, properties, permissions);


        public async Task Init()
        {
            this.native = await GattServiceProvider.CreateAsync(this.Uuid);
            if (this.native.Error != BluetoothError.Success)
                throw new ArgumentException();

            foreach (var ch in this.Characteristics.OfType<IUwpGattCharacteristic>())
            {
                await ch.Init(this.native.ServiceProvider.Service);
            }

            this.native.ServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = true,
                IsDiscoverable = true
            });
        }


        public void Stop() => this.native?.ServiceProvider.StopAdvertising();
    }
}
