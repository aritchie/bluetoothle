using System;


namespace Plugin.BluetoothLE.Server
{
    public class WriteRequest
    {
        public WriteRequest(IDevice device, byte[] value, int offset, bool isReplyNeeded)
        {
            this.Device = device;
            this.Offset = offset;
            this.IsReplyNeeded = isReplyNeeded;
            this.Value = value;
        }


        public IDevice Device { get; }
        public int Offset { get; }
        public bool IsReplyNeeded { get; }
        public byte[] Value { get; }
        public GattStatus Status { get; set; } = GattStatus.Success;
    }
}