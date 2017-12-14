using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public interface IUwpGattDescriptor : IGattDescriptor
    {
        Task Init(GattLocalCharacteristic native);
    }
}
