using System;


namespace Plugin.BluetoothLE
{
    public class DescriptorGattResult : AbstractGattResult
    {
        public DescriptorGattResult(IGattDescriptor descriptor,
                                    GattEvent gattEvent,
                                    byte[] data) : base(gattEvent, data)
        {
            this.Descriptor = descriptor;
        }


        public DescriptorGattResult(IGattDescriptor descriptor,
                                    GattEvent gattEvent,
                                    string errorMessage) : base(gattEvent, errorMessage)
        {
            this.Descriptor = descriptor;
        }


        public IGattDescriptor Descriptor { get; }
    }
}
