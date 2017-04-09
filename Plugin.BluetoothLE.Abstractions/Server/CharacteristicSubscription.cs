using System;


namespace Plugin.BluetoothLE.Server
{
    public class CharacteristicSubscription
    {
        public CharacteristicSubscription(IGattCharacteristic characteristic, IDevice device, bool isSubscribing)
        {
            this.Characteristic = characteristic;
            this.Device = device;
            this.IsSubscribing = isSubscribing;
        }


        public IGattCharacteristic Characteristic { get; }
        public IDevice Device { get; }
        public bool IsSubscribing { get; }
    }
}
