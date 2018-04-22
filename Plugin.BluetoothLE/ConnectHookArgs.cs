using System;


namespace Plugin.BluetoothLE
{
    public class ConnectHookArgs
    {
        public ConnectHookArgs(Guid serviceUuid, params Guid[] characteristicUuids)
        {
            this.ServiceUuid = serviceUuid;
            this.CharacteristicUuids = characteristicUuids;
        }


        public bool DisconnectOnUnsubscribe { get; set; } = true;
        public bool UseIndicateIfAvailable { get; set; }
        public Guid ServiceUuid { get; }
        public Guid[] CharacteristicUuids { get; }
    }
}
