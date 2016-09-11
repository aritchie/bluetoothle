using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor;


namespace Acr.Ble
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly Native native;


        public GattDescriptor(Native native, IGattCharacteristic characteristic) : base(characteristic, native.Uuid)
        {
            this.native = native;
        }


        public override IObservable<object> Write(byte[] data)
        {
            return Observable.Create<object>(async ob =>
            {
                var result = await this.native.WriteValueAsync(data.AsBuffer());
                if (result == GattCommunicationStatus.Success)
                {
                    ob.Respond(null);
                }
                else
                {
                    ob.OnError(new Exception("Not able to write to descriptor"));
                }
                return Disposable.Empty;
            });
        }


        public override IObservable<byte[]> Read()
        {
            return Observable.Create<byte[]>(async ob =>
            {
                var result = await this.native.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Not able to read descriptor"));
                }
                else
                {
                    var bytes = result.Value.ToArray();
                    ob.Respond(bytes);
                }
                return Disposable.Empty;
            });
        }
    }
}
