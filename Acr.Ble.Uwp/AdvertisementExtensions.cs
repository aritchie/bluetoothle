using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;


namespace Plugin.BluetoothLE
{
    public static class AdvertisementExtensions
    {
        public static string GetDeviceName(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(AdvertisementRecordType.CompleteLocalName);
            if (data == null)
                return adv.LocalName;

            var name = Encoding.UTF8.GetString(data);
            return name;
        }


        public static sbyte GetTxPower(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(AdvertisementRecordType.TxPowerLevel);
            return data == null ? (sbyte)0 : (sbyte) data[0];
        }


        public static byte[] GetSectionDataOrNull(this BluetoothLEAdvertisement adv, AdvertisementRecordType recType)
        {
            var section = adv.DataSections.FirstOrDefault(x => x.DataType == (byte) recType);
            var data = section?.Data.ToArray();
            return data;
        }
    }
}
