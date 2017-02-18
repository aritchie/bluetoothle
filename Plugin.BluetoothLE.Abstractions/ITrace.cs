//using System;


//namespace Acr.Ble
//{
//    public interface ITrace
//    {
//        // all available at the adapter level
//        IObservable<AdapterStatus> WhenAdapterStatusChanged();
//        IObservable<ScanConfig> WhenScanStarted();
//        IObservable<object> WhenScanStopped();
//        IObservable<IScanResult> WhenScan();
//        IObservable<IDevice> WhenDeviceStatusChanged();

//        IObservable<IGattService> WhenServiceDiscovered();
//        IObservable<IGattCharacteristic> WhenCharacteristicDiscovered();
//        IObservable<CharacteristicResult> WhenCharacteristicRead();
//        IObservable<CharacteristicResult> WhenCharacteristicWritten();
//        IObservable<CharacteristicResult> WhenCharacteristicNotified();
//        IObservable<IGattDescriptor> WhenDescriptorDiscovered();
//        IObservable<DescriptorResult> WhenDescriptorRead();
//        IObservable<DescriptorResult> WhenDescriptorWritten();
//    }
//}
