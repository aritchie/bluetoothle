using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        public GattCharacteristic(Native native, IGattService service) : base(service, native.Uuid, (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.Native = native;
        }


        public Native Native { get; }

        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(async ob =>
            {
                var result = await this.Native.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                //if (result.Status)
                foreach (var dnative in result.Descriptors)
                {
                    var descriptor = new GattDescriptor(dnative, this);
                    ob.OnNext(descriptor);
                }
                return Disposable.Empty;
            })
            .Replay();
            return this.descriptorOb;
        }


        public override IObservable<CharacteristicResult> Read()
        {
            this.AssertRead();

            return Observable.Create<CharacteristicResult>(async ob =>
            {
                var result = await this.Native.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Error reading characteristics - " + result.Status));
                }
                else
                {
                    var bytes = result.Value.ToArray();
                    this.Value = bytes;
                    //this.ReadSubject.OnNext(bytes);
                    //ob.Respond(bytes);
                }
                return Disposable.Empty;
            });
        }


        public override IObservable<BleWriteSegment> BlobWrite(Stream stream, bool reliableWrite)
        {
            var trans = new GattReliableWriteTransaction();

            return base.BlobWrite(stream, reliableWrite);
        }



        public override async void WriteWithoutResponse(byte[] value)
        {
            this.AssertWrite(false);
            await this.Native.WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithoutResponse);
            this.Value = value;
            this.WriteSubject.OnNext(new CharacteristicResult(this, CharacteristicEvent.Write, this.Value));
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            // TODO: reliable write
            this.AssertWrite(false);

            return Observable.Create<CharacteristicResult>(async ob =>
            {
                var result = await this.Native.WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithResponse);

                if (result != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Error writing characteristic"));
                }
                else
                {
                    this.Value = value;
                    //this.WriteSubject.OnNext(this.Value);

                    ob.Respond(null);
                }
                return Disposable.Empty;
            });
        }


        IObservable<CharacteristicResult> notificationOb;
        public override IObservable<CharacteristicResult> SubscribeToNotifications()
        {
            this.AssertNotify();

            this.notificationOb = this.notificationOb ?? Observable.Create<CharacteristicResult>(async ob =>
            {
                //var trigger = new GattCharacteristicNotificationTrigger(this.native);

                var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
                {
                    if (sender.Equals(this.Native))
                    {
                        var bytes = args.CharacteristicValue.ToArray();
                        //ob.OnNext(bytes);
                        //this.NotifySubject.OnNext(bytes);
                    }
                });
                this.Native.ValueChanged += handler;

                var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status != GattCommunicationStatus.Success)
                {
                    ob.OnError(new Exception("Could not subscribe to notifications"));
                }

                return () =>
                {
                    this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None).GetResults();
                    this.Native.ValueChanged -= handler;
                };
            });
            return this.notificationOb;
        }
    }
}
