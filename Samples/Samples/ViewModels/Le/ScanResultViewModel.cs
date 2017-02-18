using System;
using System.Diagnostics;
using Plugin.BluetoothLE;
using ReactiveUI.Fody.Helpers;


namespace Samples.ViewModels.Le
{
    public class ScanResultViewModel : AbstractViewModel
    {
        //IDisposable nameOb;
        public IDevice Device { get; private set; }
        [Reactive] public string Name { get; private set; }
        [Reactive] public bool IsConnected { get; set; }
        [Reactive] public Guid Uuid { get; private set; }
        [Reactive] public int Rssi { get; private set; }
        [Reactive] public bool IsConnectable { get; private set; }
        [Reactive] public int ServiceCount { get; private set; }
        [Reactive] public string ManufacturerData { get; private set; }
        [Reactive] public string LocalName { get; private set; }
        [Reactive] public int TxPower { get; private set; }


        public bool TrySet(IScanResult result)
        {
            var response = false;

            if (this.Uuid == Guid.Empty)
            {
                this.Device = result.Device;
                this.Uuid = this.Device.Uuid;
                //this.nameOb = result
                //    .Device
                //    .WhenNameUpdated()
                //    .Subscribe(x => this.Name = x);

                response = true;
            }

            try 
            {
                if (this.Uuid == result.Device.Uuid)
                {
                    response = true;

                    this.Name = result.Device.Name;
                    this.Rssi = result.Rssi;

                    var ad = result.AdvertisementData;
                    this.ServiceCount = ad.ServiceUuids?.Length ?? 0;
                    this.IsConnectable = ad.IsConnectable;
                    this.LocalName = ad.LocalName;
                    this.TxPower = ad.TxPower;
                    this.ManufacturerData = ad.ManufacturerData == null
                        ? null
                        : BitConverter.ToString(ad.ManufacturerData);
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex.ToString());
            }
            return response;
        }
    }
}