using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Radios;


namespace Plugin.BluetoothLE
{
    public class AdapterScanner : IAdapterScanner
    {
        public IObservable<IAdapter> FindAdapters()
            => Observable.Create<IAdapter>(async ob =>
            {
                var radios = await Radio.GetRadiosAsync();
                radios
                    .Where(x => x.Kind == RadioKind.Bluetooth)
                    .ToList()
                    .ForEach(x =>
                    {
                        var adapter = new Adapter(x);
                        ob.OnNext(adapter);
                    });

                ob.OnCompleted();
                return Disposable.Empty;
            });
    }
}
