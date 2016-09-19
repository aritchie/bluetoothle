using System;


namespace Acr.Ble.Plugins
{
    public class BleLogEvent
    {
        public BleLogEvent(BleLogFlags flag, Guid? uuid, string details) 
        {
            this.Category = flag;
            this.Uuid = uuid;
            this.Details = details;
        }


        public Guid? Uuid { get; }
        public BleLogFlags Category { get; }
        public string Details { get; }
    }
}
