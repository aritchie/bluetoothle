using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.UI.Core;
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

            return Observable.FromAsync(async ct =>
            {
                var result = await this.Native
                    .ReadValueAsync(BluetoothCacheMode.Uncached)
                    .AsTask(ct);

                if (result.Status != GattCommunicationStatus.Success)
                    throw new Exception("Error reading characteristics - " + result.Status);

                var bytes = result.Value.ToArray();
                this.Value = bytes;
                var r = new CharacteristicResult(this, CharacteristicEvent.Read, bytes);
                this.ReadSubject.OnNext(r);

                return r;
            });
        }


        //public override IObservable<BleWriteSegment> BlobWrite(Stream stream, bool reliableWrite)
        //{
        //    var trans = new GattReliableWriteTransaction();
        //    // TODO
        //    return base.BlobWrite(stream, reliableWrite);
        //}



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

            return Observable.FromAsync(async ct =>
            {
                var result = await this.Native
                    .WriteValueAsync(value.AsBuffer(), GattWriteOption.WriteWithResponse)
                    .AsTask(ct);

                if (result != GattCommunicationStatus.Success)
                    throw new Exception("Error writing characteristic");

                this.Value = value;
                var r = new CharacteristicResult(this, CharacteristicEvent.Write, value);
                this.WriteSubject.OnNext(r);
                return r;
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
                        var result = new CharacteristicResult(this, CharacteristicEvent.Notification, bytes);
                        ob.OnNext(result);
                        this.NotifySubject.OnNext(result);
                    }
                });
                this.Native.ValueChanged += handler;

                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        if (status != GattCommunicationStatus.Success)
                        {
                            ob.OnError(new Exception("Could not subscribe to notifications"));
                        }
                    }
                );

                return async () =>
                {
                    this.Native.ValueChanged -= handler;
                    await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                        }
                        catch (Exception e)
                        {
                            //System.Console.WriteLine(e);
                            System.Diagnostics.Debug.WriteLine(e.ToString());
                        }
                    });

                };
            });
            return this.notificationOb;
        }
    }
}
