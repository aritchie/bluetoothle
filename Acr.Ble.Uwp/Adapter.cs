using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;


namespace Acr.Ble
{
    public class Adapter : IAdapter
    {
        readonly DeviceManager deviceManager;
        readonly Lazy<Radio> radio;


        public Adapter()
        {
            this.deviceManager = new DeviceManager();
            this.radio = new Lazy<Radio>(() =>
                Radio
                    .GetRadiosAsync()
                    .AsTask()
                    .Result
                    .FirstOrDefault(x => x.Kind == RadioKind.Bluetooth)
            );
        }


        public bool IsScanning { get; private set; }
        public AdapterStatus Status
        {
            get
            {
                if (this.radio == null)
                    return AdapterStatus.Unsupported;

                switch (this.radio.Value.State)
                {
                    case RadioState.Disabled:
                    case RadioState.Off:
                        return AdapterStatus.PoweredOff;

                    case RadioState.Unknown:
                        return AdapterStatus.Unknown;

                    default:
                        return AdapterStatus.PoweredOn;
                }
            }
        }


        public IObservable<bool> WhenScanningStatusChanged()
        {
            throw new NotImplementedException();
        }


        public IObservable<IScanResult> Scan(ScanFilter filter = null)
        {
            return Observable.Create<IScanResult>(ob =>
            {

                //var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
                //await BluetoothLEDevice.FromIdAsync(devices[0].Id)
                //DeviceInformation.FindAllAsync(GattDeviceService.)
                var handler = new TypedEventHandler
                    <BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>(
                    (sender, args) =>
                    {
                        try
                        {
                            var device = this.deviceManager.GetDevice(args);
                            var adData = new AdvertisementData(args.Advertisement);
                            var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                            ob.OnNext(scanResult);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to get device - {ex}");
                        }
                    });

                var scanner = new BluetoothLEAdvertisementWatcher();
                if (filter != null)
                {
                    //this.currentScanner.AdvertisementFilter = new BluetoothLEAdvertisementFilter
                    //{
                    //    Advertisement = new BluetoothLEAdvertisement
                    //    {
                    //        LocalName = "",
                    //        //ServiceUuids =
                    //    }
                    //};
                }
                scanner.Received += handler;
                scanner.Start();
                this.IsScanning = true;

                return () =>
                {
                    scanner.Stop();
                    scanner.Received -= handler;
                    this.IsScanning = false;
                };
            });
        }


        public IObservable<AdapterStatus> WhenStatusChanged()
        {
            return Observable.Create<AdapterStatus>(ob =>
            {
                ob.OnNext(this.Status);
                var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    ob.OnNext(this.Status)
                );
                this.radio.Value.StateChanged += handler;
                return () => this.radio.Value.StateChanged -= handler;
            });
        }


        public IObservable<IDevice> WhenDeviceStatusChanged()
        {
            return Observable.Create<IDevice>(ob =>
            {

                return () => {};
            });
        }
    }
}
