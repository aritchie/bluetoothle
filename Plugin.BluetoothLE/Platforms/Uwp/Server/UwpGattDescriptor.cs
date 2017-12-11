using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public class UwpGattDescriptor : AbstractGattDescriptor, IUwpGattDescriptor
    {
        GattLocalDescriptor native;


        public UwpGattDescriptor(IGattCharacteristic characteristic, Guid descriptorUuid, byte[] value) : base(characteristic, descriptorUuid, value)
        {
        }


        public async Task Init(GattLocalCharacteristic characteristic)
        {
            var result = await characteristic.CreateDescriptorAsync(
                this.Uuid,
                new GattLocalDescriptorParameters
                {
                    ReadProtectionLevel = GattProtectionLevel.Plain,
                    WriteProtectionLevel = GattProtectionLevel.Plain
                    //Vale = null
                }
            );
            this.native = result.Descriptor;
        }
    }
}
