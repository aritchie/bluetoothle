using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;


namespace Plugin.BluetoothLE
{
    public class AdapterScanner : IAdapterScanner
    {
        public bool IsSupported => true;


        public IObservable<IAdapter> FindAdapters() => Observable.Create<IAdapter>(async ob =>
        {
            var devices = await DeviceInformation.FindAllAsync(BluetoothAdapter.GetDeviceSelector());
            foreach (var dev in devices)
            {
                Log.Info($"Adapter", "found - {dev.Name} ({dev.Kind} - {dev.Id})");

                var native = await BluetoothAdapter.FromIdAsync(dev.Id);
                if (native.IsLowEnergySupported)
                {
                    var radio = await native.GetRadioAsync();
                    var adapter = new Adapter(native, radio);
                    ob.OnNext(adapter);
                }
            }
            ob.OnCompleted();
            return Disposable.Empty;
        });
    }
}
