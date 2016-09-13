using System;
using Acr.Ble;
using Autofac;


namespace Samples.Tasks
{
    public class LogTask : IStartable
    {
        readonly object syncLock = new object();
        readonly IAdapter adapter;


        public LogTask(IAdapter adapter)
        {
            this.adapter = adapter;
        }


        public void Start()
        {
            this.adapter
                .ScanListen()
                .Subscribe(scanResult =>
                {

                });

            this.adapter
                .WhenScanningStatusChanged()
                .Subscribe(status =>
                {

                });

            this.adapter
                .WhenDeviceStatusChanged()
                .Subscribe(device =>
                {
                    //device.WhenServiceDiscovered()
                    //device.WhenAnyCharacteristic()
                    //device.WhenyAnyDescriptor();
                });
        }


        void WriteLog(string message)
        {
            var log = $"[{DateTime.Now:T}] {message}";
            lock(this.syncLock)
            {

            }
        }
    }
}
