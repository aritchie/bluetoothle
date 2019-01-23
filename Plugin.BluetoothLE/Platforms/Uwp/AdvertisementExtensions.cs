﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Plugin.BluetoothLE.Server;
using Windows.Devices.Bluetooth.Advertisement;


namespace Plugin.BluetoothLE
{
    public static class AdvertisementExtensions
    {
        public static string GetDeviceName(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(BluetoothLEAdvertisementDataTypes.CompleteLocalName);
            if (data == null)
                return adv.LocalName;

            var name = Encoding.UTF8.GetString(data);
            return name;
        }


        public static sbyte GetTxPower(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(BluetoothLEAdvertisementDataTypes.TxPowerLevel);
            return data == null ? (sbyte)0 : (sbyte) data[0];
        }


        public static ManufacturerData[] GetManufacturerSpecificData(this BluetoothLEAdvertisement adv)
            => adv.ManufacturerData.Select(md => new ManufacturerData(md.CompanyId, md.Data.ToArray())).ToArray();


        static byte[] GetSectionDataOrNull(this BluetoothLEAdvertisement adv, byte recType)
        {
            var section = adv.DataSections.FirstOrDefault(x => x.DataType == recType);
            var data = section?.Data.ToArray();
            return data;
        }
    }
}
