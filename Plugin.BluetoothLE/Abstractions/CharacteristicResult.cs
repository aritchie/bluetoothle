using System;
namespace Plugin.BluetoothLE
{
    public class CharacteristicResult
    {
        public CharacteristicResult(IGattCharacteristic characteristic, CharacteristicEvent eventType, byte[] data)
        {
            this.Characteristic = characteristic;
            this.Event = eventType;
            this.Data = data;
        }


        public IGattCharacteristic Characteristic { get; }
        public CharacteristicEvent Event { get; }
        public byte[] Data { get; }
    }
}
