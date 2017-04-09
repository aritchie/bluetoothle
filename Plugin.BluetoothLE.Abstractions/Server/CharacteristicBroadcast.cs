using System;


namespace Plugin.BluetoothLE.Server
{
    public class CharacteristicBroadcast
    {

        public CharacteristicBroadcast(IDevice device, IGattCharacteristic characteristic, byte[] data, bool indication, bool success)
        {
            this.Device = device;
            this.Characteristic = characteristic;
            this.Data = data;
            this.Indication = indication;
            this.Success = success;
        }


        public bool Success { get; }
        public IDevice Device { get; }
        public IGattCharacteristic Characteristic { get; }
        public byte[] Data { get; }
        public bool Indication { get; }
    }
}
