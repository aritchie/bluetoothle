using System;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        public override IObservable<bool> WhenScanningStatusChanged()
        {
            throw new NotImplementedException();
        }

        public override IObservable<IScanResult> Scan(ScanConfig config = null)
        {
            throw new NotImplementedException();
        }

        public override IObservable<IScanResult> ScanListen()
        {
            throw new NotImplementedException();
        }

        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}
