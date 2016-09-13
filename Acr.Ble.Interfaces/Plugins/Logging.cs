using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Acr.Ble.Plugins
{
    public static class Logging
    {
        [Flags]
        public enum BleLogFlags
        {
            AdapterEvents = 1,
            // advertisement data?
            DeviceEvents = 2,
            CharacteristicNotifications = 4
        }


        public static IObservable<string> WhenActionOccurs(this IAdapter adapter, BleLogFlags flags = BleLogFlags.AdapterEvents)
        {
            return Observable.Create<string>(ob =>
            {
                var list = new List<IDisposable>();
                list.Add(adapter
                    .WhenStatusChanged()
                    .Subscribe(status =>
                        ob.OnNext($"[Adapter] status changed to {status}")
                    )
                );

                list.Add(adapter
                    .ScanListen()
                    .Subscribe(scanResult =>
                    {
                        //ob.OnNext($"[Scan]{scanResult.AdvertisementData}");
                    })
                );

                list.Add(adapter
                    .WhenScanningStatusChanged()
                    .Subscribe(status =>
                        ob.OnNext($"[Adapter] Scanning status changed to {status}")
                    )
                );


                list.Add(adapter
                    .WhenDeviceStatusChanged()
                    .Subscribe(device =>
                    {
                        list.Add(device
                            .WhenServiceDiscovered()
                            .Subscribe(serv =>
                                ob.OnNext($"[Service] {serv.Uuid} discovered")
                            )
                        );
                        list.Add(device
                            .WhenAnyCharacteristic()
                            .Subscribe(ch =>
                            {
                                ob.OnNext($"[Characteristic] {ch.Uuid} discovered");

                                if (ch.CanNotify())
                                {
                                    list.Add(ch
                                        .WhenNotificationOccurs()
                                        .Subscribe(bytes =>
                                        {
                                            var value = BitConverter.ToString(bytes);
                                            ob.OnNext($"[Characteristic] {ch.Uuid} notification - Value: {value}");
                                        })
                                    );
                                }
                            })
                        );
                        list.Add(device
                            .WhenyAnyDescriptor()
                            .Subscribe(desc =>
                                ob.OnNext($"[Descriptor] {desc.Uuid} discovered")
                            )
                        );
                        // TODO: characteristic read/writes
                        // TODO: descriptor read/writes
                    })
                );
                return () =>
                {
                    foreach (var dispose in list)
                        dispose.Dispose();
                };
            });
        }
    }
}
