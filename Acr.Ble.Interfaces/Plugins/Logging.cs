using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Acr.Ble.Plugins
{

    [Flags]
    public enum BleLogFlags
    {
        AdapterEvents = 1,
        ScanEvents = 2,
        DeviceEvents = 4,
        CharacteristicNotifications = 8,
        All = AdapterEvents | ScanEvents | DeviceEvents | CharacteristicNotifications
        // TODO: characteristic read/writes
        // TODO: descriptor read/writes
    }


    public static class Logging
    {
        public static IObservable<string> WhenActionOccurs(this IAdapter adapter, BleLogFlags flags = BleLogFlags.AdapterEvents | BleLogFlags.DeviceEvents)
        {
            return Observable.Create<string>(ob =>
            {
                var list = new List<IDisposable>();
                var deviceEvents = new Dictionary<Guid, List<IDisposable>>();

                if (flags.HasFlag(BleLogFlags.AdapterEvents))
                {
                    list.Add(adapter
                        .WhenStatusChanged()
                        .Subscribe(status =>
                            ob.OnNext($"[Adapter] status changed to {status}")
                        )
                    );
                }
                if (flags.HasFlag(BleLogFlags.ScanEvents))
                {
                    list.Add(adapter
                        .ScanListen()
                        .Subscribe(scanResult =>
                            ob.OnNext($"[Scan] {scanResult.Device.Name} ({scanResult.Device}) with RSSI of {scanResult.Rssi}")
                        )
                    );

                    list.Add(adapter
                        .WhenScanningStatusChanged()
                        .Subscribe(status =>
                            ob.OnNext($"[Adapter] Scanning status changed to {status}")
                        )
                    );
                }

                if (flags.HasFlag(BleLogFlags.DeviceEvents))
                {
                    list.Add(adapter
                        .WhenDeviceStatusChanged()
                        .Subscribe(device =>
                        {
                            ob.OnNext($"[Device] State changed to {device.Status}");
                            lock(deviceEvents)
                            {
                                if (device.Status == ConnectionStatus.Connected)
                                {
                                    var reg = new List<IDisposable>();
                                    HookDeviceEvents(reg, device, ob, flags.HasFlag(BleLogFlags.CharacteristicNotifications));
                                    deviceEvents.Add(device.Uuid, reg);
                                }
                                else if (deviceEvents.ContainsKey(device.Uuid))
                                {
                                    var reg = deviceEvents[device.Uuid];
                                    foreach (var item in reg)
                                        item.Dispose();

                                    deviceEvents.Remove(device.Uuid);
                                }
                            }
                        })
                    );
                }

                return () =>
                {
                    foreach (var dispose in list)
                        dispose.Dispose();
                };
            });
        }


        static void HookDeviceEvents(IList<IDisposable> registrations, IDevice device, IObserver<string> ob, bool includeCharacteristicNotifications)
        {
            registrations.Add(device
                .WhenServiceDiscovered()
                .Subscribe(serv =>
                    ob.OnNext($"[Service] {serv.Uuid} discovered")
                )
            );
            registrations.Add(device
                .WhenAnyCharacteristic()
                .Subscribe(ch =>
                {
                    ob.OnNext($"[Characteristic] {ch.Uuid} discovered");

                    if (includeCharacteristicNotifications && ch.CanNotify())
                    {
                        registrations.Add(ch
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
            registrations.Add(device
                .WhenyAnyDescriptor()
                .Subscribe(desc =>
                    ob.OnNext($"[Descriptor] {desc.Uuid} discovered")
                )
            );
        }
    }
}

