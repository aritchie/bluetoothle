using System;


namespace Acr.Ble.Plugins
{
    public class BleLogEvent
    {
        public BleLogEvent(BleLogFlags flag, Guid? uuid, byte[] data, string details) 
        {
            this.Category = flag;
            this.Uuid = uuid;
            this.Data = data;
            this.Details = details;
        }


        public Guid? Uuid { get; }
        public BleLogFlags Category { get; }
        public byte[] Data { get; }
        public string Details { get; }
    }
}
