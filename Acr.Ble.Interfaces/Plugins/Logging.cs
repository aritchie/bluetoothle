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
        CharacteristicEvents = 8,
        CharacteristicNotifications = 16,
        DescriptorEvents = 32,
        All = AdapterEvents | ScanEvents | DeviceEvents | CharacteristicEvents | CharacteristicNotifications | DescriptorEvents
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
                            ob.OnNext($"[Scan] {scanResult.Device.Name} ({scanResult.Device.Uuid}) with RSSI of {scanResult.Rssi}")
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
                                    HookDeviceEvents(reg, device, ob, flags);
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


        static void HookDeviceEvents(IList<IDisposable> registrations, IDevice device, IObserver<string> ob, BleLogFlags flags)
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

                    if (flags.HasFlag(BleLogFlags.CharacteristicEvents))
                    {
                        registrations.Add(ch
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, "Characteristic", "written", ch.Uuid, bytes))
                        );
                        registrations.Add(ch
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, "Characteristic", "read", ch.Uuid, bytes))
                        );
                    }
                    if (flags.HasFlag(BleLogFlags.CharacteristicNotifications) && ch.CanNotify())
                    {
                        registrations.Add(ch
                            .WhenNotificationReceived()
                            .Subscribe(bytes => Write(ob, "Characteristic", "notifications", ch.Uuid, bytes))
                        );
                    }
                })
            );
            registrations.Add(device
                .WhenyAnyDescriptor()
                .Subscribe(desc =>
                {
                    ob.OnNext($"[Descriptor]({desc.Uuid}) discovered");
                    if (flags.HasFlag(BleLogFlags.DescriptorEvents))
                    {
                        registrations.Add(desc
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, "Descriptor", "read", desc.Uuid, bytes))
                        );
                        registrations.Add(desc
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, "Descriptor", "written", desc.Uuid, bytes))
                        );
                    }
                })
            );
        }


        static void Write(IObserver<string> ob, string category, string subcategory, Guid uuid, byte[] bytes)
        {
            var value = BitConverter.ToString(bytes);
            var msg = $"[{category}]({uuid}) {subcategory}: {value}";
            ob.OnNext(msg);
        }
    }
}

