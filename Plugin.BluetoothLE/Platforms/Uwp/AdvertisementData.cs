﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;


namespace Plugin.BluetoothLE
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly BluetoothLEAdvertisementReceivedEventArgs adData;
        readonly Lazy<Guid[]> serviceUuids;
        readonly Lazy<ManufacturerData[]> manufacturerData;
        readonly Lazy<int> txPower;


        public AdvertisementData(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            this.adData = args;

            this.manufacturerData = new Lazy<ManufacturerData[]>(() => args.Advertisement.GetManufacturerSpecificData());
            this.serviceUuids = new Lazy<Guid[]>(() => args.Advertisement.ServiceUuids.ToArray());
            this.txPower = new Lazy<int>(() => args.Advertisement.GetTxPower());
        }


        public BluetoothLEAdvertisement Native => this.adData.Advertisement;
        public ulong BluetoothAddress => this.adData.BluetoothAddress;
        public string LocalName => this.adData.Advertisement.LocalName;
        public bool IsConnectable => this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableDirected ||
                                     this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableUndirected;

        public IReadOnlyList<byte[]> ServiceData { get; } = null;
        public ManufacturerData[] ManufacturerData => this.manufacturerData.Value;
        public Guid[] ServiceUuids => this.serviceUuids.Value;
        public int TxPower => this.txPower.Value;
    }
}
