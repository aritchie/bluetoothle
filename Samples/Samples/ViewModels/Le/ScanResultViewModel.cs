using System;
using System.Diagnostics;
using Plugin.BluetoothLE;
using ReactiveUI;


namespace Samples.ViewModels.Le
{
    public class ScanResultViewModel : AbstractViewModel
    {
        //IDisposable nameOb;
        public IDevice Device { get; private set; }


        string name;
        public string Name
        {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }


        bool connected;
        public bool IsConnected
        {
            get => this.connected;
            set => this.RaiseAndSetIfChanged(ref this.connected, value);
        }


        Guid uuid;
        public Guid Uuid
        {
            get => this.uuid;
            private set => this.RaiseAndSetIfChanged(ref this.uuid, value);
        }


        int rssi;
        public int Rssi
        {
            get => this.rssi;
            private set => this.RaiseAndSetIfChanged(ref this.rssi, value);
        }


        bool connectable;
        public bool IsConnectable
        {
            get => this.connectable;
            private set => this.RaiseAndSetIfChanged(ref this.connectable, value);
        }


        int serviceCount;
        public int ServiceCount
        {
            get => this.serviceCount;
            private set => this.RaiseAndSetIfChanged(ref this.serviceCount, value);
        }


        string manufacturerData;
        public string ManufacturerData
        {
            get => this.manufacturerData;
            private set => this.RaiseAndSetIfChanged(ref this.manufacturerData, value);
        }


        string localName;
        public string LocalName
        {
            get => this.localName;
            private set => this.RaiseAndSetIfChanged(ref this.localName, value);
        }


        int txPower;
        public int TxPower
        {
            get => this.txPower;
            private set => this.RaiseAndSetIfChanged(ref this.txPower, value);
        }


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