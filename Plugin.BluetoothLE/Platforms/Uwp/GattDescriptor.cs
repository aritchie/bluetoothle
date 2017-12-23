using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly Native native;


        public GattDescriptor(Native native, IGattCharacteristic characteristic) : base(characteristic, native.Uuid)
        {
            this.native = native;
        }


        byte[] value;
        public override byte[] Value => this.value;


        public override IObservable<DescriptorGattResult> Write(byte[] data) => Observable.FromAsync(async _ =>
        {
            var status = await this.native.WriteValueAsync(data.AsBuffer());
            var result = status == GattCommunicationStatus.Success
                ? this.ToResult(GattEvent.Write, data)
                : this.ToResult(GattEvent.WriteError, status.ToString());

            return result;
        });


        public override IObservable<DescriptorGattResult> Read() => Observable.FromAsync(async _ =>
        {
            var result = await this.native.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
                return this.ToResult(GattEvent.WriteError, result.Status.ToString());

            this.value = result.Value.ToArray();
            return this.ToResult(GattEvent.Read, this.value);
        });
    }
}
