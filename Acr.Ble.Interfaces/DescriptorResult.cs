using System;


namespace Acr.Ble
{
    public class DescriptorResult
    {
        public DescriptorResult(IGattDescriptor descriptor, DescriptorEvent eventType, byte[] data)
        {
            this.Descriptor = descriptor;
            this.Event = eventType;
            this.Data = data;
        }


        public IGattDescriptor Descriptor { get; }
        public DescriptorEvent Event { get; }
        public byte[] Data { get; }
    }
}
