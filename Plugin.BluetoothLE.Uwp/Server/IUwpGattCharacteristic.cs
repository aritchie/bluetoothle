using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public interface IUwpGattCharacteristic : Windows.Devices.Bluetooth.GenericAttributeProfile.IGattCharacteristic
    {
        Task Init(GattLocalService gatt);
    }
}
