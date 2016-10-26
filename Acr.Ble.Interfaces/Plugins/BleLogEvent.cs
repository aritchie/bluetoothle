using System;


namespace Acr.Ble.Plugins
{
    public class BleLogEvent
    {
        public BleLogEvent(IDevice device, BleLogFlags flag, Guid? uuid, byte[] data, string details)
        {
            this.Device = device;
            this.Category = flag;
            this.Uuid = uuid;
            this.Data = data;
            this.Details = details;
        }


        public IDevice Device { get; }
        public Guid? Uuid { get; }
        public BleLogFlags Category { get; }
        public byte[] Data { get; }
        public string Details { get; }
    }
}
