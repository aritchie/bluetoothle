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
                if (result != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Not able to write to descriptor"));
                }
                else
                {
                    this.Value = data;
                    this.WriteSubject.OnNext(this.Value);
                    ob.Respond(null);
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
                    this.Value = bytes;
                    this.ReadSubject.OnNext(this.Value);
                    ob.Respond(bytes);
                }
                return Disposable.Empty;
            });
        }
    }
}
