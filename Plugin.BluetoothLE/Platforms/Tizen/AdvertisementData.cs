using System;
using System.Collections.Generic;
using System.Linq;
using Tizen.Network.Bluetooth;


namespace Plugin.BluetoothLE
{
    public class AdvertisementData : IAdvertisementData
    {
        readonly BluetoothLeDevice native;


        public AdvertisementData(BluetoothLeDevice native)
        {
            this.native = native;
        }


        public string LocalName { get; }
        public bool IsConnectable { get; }
        public IReadOnlyList<byte[]> ServiceData { get; }
        public byte[] ManufacturerData => this.native.ManufacturerData?.Data;
        public Guid[] ServiceUuids => this.native.ServiceUuid?.Select(x => new Guid(x)).ToArray();
        public int TxPower => this.native.TxPowerLevel;
    }
}
