using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Plugin.BluetoothLE.Server
{
    public interface IUwpGattCharacteristic : IGattCharacteristic
    {
        Task Init(GattLocalService gatt);
    }
}
