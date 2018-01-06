using System;


namespace Plugin.BluetoothLE
{
    public class CharacteristicGattResult : AbstractGattResult
    {
        public CharacteristicGattResult(IGattCharacteristic characteristic,
                                        GattEvent gattEvent,
                                        byte[] data) : base(gattEvent, data)
        {
            this.Characteristic = characteristic;
        }


        public CharacteristicGattResult(IGattCharacteristic characteristic,
                                        GattEvent gattEvent,
                                        string errorMessage) : base(gattEvent, errorMessage)
        {
            this.Characteristic = characteristic;
        }


        public IGattCharacteristic Characteristic { get; }
    }
}
