using System;


namespace Acr.Ble
{
    public class CharacteristicNotification
    {
        public CharacteristicNotification(IGattCharacteristic characteristic, byte[] value)
        {
            this.Characteristic = characteristic;
            this.Value = value;
        }


        public IGattCharacteristic Characteristic { get; }
        public byte[] Value { get; }
    }
}
