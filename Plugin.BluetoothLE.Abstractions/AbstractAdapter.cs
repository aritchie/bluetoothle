using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.BluetoothLE.Server;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractAdapter : IAdapter
    {
        public virtual string DeviceName { get; protected set; }
        public virtual IGattServer CreateGattServer()
        {
            throw new NotImplementedException();
        }


        public virtual AdapterFeatures Features { get; }
        public virtual IDevice GetKnownDevice(Guid deviceId)
        {
            throw new NotImplementedException();
        }


        public virtual AdapterStatus Status { get; }
        public virtual bool IsScanning { get; }
        public virtual IEnumerable<IDevice> GetConnectedDevices()
        {
            throw new NotImplementedException();
        }


        public virtual IEnumerable<IDevice> GetPairedDevices()
        {
            throw new NotImplementedException();
        }


        public virtual IObservable<bool> WhenScanningStatusChanged()
        {
            throw new NotImplementedException();
        }


        public IObservable<IScanResult> Scan(ScanConfig config = null)
        {
            throw new NotImplementedException();
        }


        public abstract IObservable<IScanResult> ScanListen();


        public virtual IObservable<AdapterStatus> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }


        public virtual IObservable<IDevice> WhenDeviceStatusChanged()
        {
            throw new NotImplementedException();
        }


        public virtual void OpenSettings()
        {
            throw new NotImplementedException();
        }


        public virtual void SetAdapterState(bool enable)
        {
            throw new NotImplementedException();
        }


        public virtual IObservable<IDevice> WhenDeviceStateRestored()
        {
            throw new NotImplementedException();
        }
    }
}
