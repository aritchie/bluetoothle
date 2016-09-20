using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;


namespace Acr.Ble.Plugins
{
    public static class Logging
    {
        public static IObservable<BleLogEvent> WhenActionOccurs(this IAdapter adapter, BleLogFlags flags = BleLogFlags.AdapterStatus | BleLogFlags.DeviceStatus)
        {
            return Observable.Create<BleLogEvent>(ob =>
            {
                var list = new List<IDisposable>();
                var deviceEvents = new Dictionary<Guid, List<IDisposable>>();

                if (flags.HasFlag(BleLogFlags.AdapterStatus))
                {
                    list.Add(adapter
                        .WhenStatusChanged()
                        .Subscribe(status => Write(ob, BleLogFlags.AdapterStatus, null, $"Changed to {status}"))
                    );
                }
                if (flags.HasFlag(BleLogFlags.AdapterScanResults))
                {
                    list.Add(adapter
                        .ScanListen()
                        .Subscribe(scanResult => Write(ob, BleLogFlags.AdapterScanResults, null, $"Device: {scanResult.Device.Uuid} - RSSI: {scanResult.Rssi}"))
                    );
                }
                if (flags.HasFlag(BleLogFlags.AdapterScanStatus))
                {
                    list.Add(adapter
                        .WhenScanningStatusChanged()
                        .Subscribe(status => Write(ob, BleLogFlags.AdapterScanStatus, null, $"Changed to {status}"))
                    );
                }
 
                list.Add(adapter
                    .WhenDeviceStatusChanged()
                    .Subscribe(device =>
                    {
                        if (flags.HasFlag(BleLogFlags.DeviceStatus))
                            Write(ob, BleLogFlags.DeviceStatus, device.Uuid, $"Changed to {device.Status}");

                        lock(deviceEvents)
                        {
                            switch (device.Status) 
                            {
                                case ConnectionStatus.Disconnected:
                                    if (deviceEvents.ContainsKey(device.Uuid))
                                    {
                                        var registration = deviceEvents[device.Uuid];
                                        foreach (var item in registration)
                                            item.Dispose();

                                        deviceEvents.Remove(device.Uuid);
                                    } 
                                    break;

                                case ConnectionStatus.Connected:
                                    var reg = new List<IDisposable>();
                                    HookDeviceEvents(reg, device, ob, flags);
                                    deviceEvents.Add(device.Uuid, reg);
                                    break;
                            }
                        }
                    })
                );

                return () =>
                {
                    foreach (var dispose in list)
                        dispose.Dispose();
                };
            });
        }


        static void HookDeviceEvents(IList<IDisposable> registrations, IDevice device, IObserver<BleLogEvent> ob, BleLogFlags flags)
        {
            if (flags.HasFlag(BleLogFlags.ServiceDiscovered))
            {
                registrations.Add(device
                    .WhenServiceDiscovered()
                    .Subscribe(serv => Write(ob, BleLogFlags.ServiceDiscovered, serv.Uuid, String.Empty))
                );
            }
            registrations.Add(device
                .WhenAnyCharacteristicDiscovered()
                .Subscribe(ch =>
                {
                    if (flags.HasFlag(BleLogFlags.CharacteristicDiscovered))
                        ob.OnNext(new BleLogEvent(BleLogFlags.CharacteristicDiscovered, ch.Uuid, String.Empty));
                      
                    if (flags.HasFlag(BleLogFlags.CharacteristicRead))
                        registrations.Add(ch
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, BleLogFlags.CharacteristicRead, ch.Uuid, bytes))
                        );
                    
                    if (flags.HasFlag(BleLogFlags.CharacteristicWrite))                    
                        registrations.Add(ch
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, BleLogFlags.CharacteristicWrite, ch.Uuid, bytes))
                        );
                    
                    if (flags.HasFlag(BleLogFlags.CharacteristicNotify) && ch.CanNotify())                    
                        registrations.Add(ch
                            .WhenNotificationReceived()
                            .Subscribe(bytes => Write(ob, BleLogFlags.CharacteristicNotify, ch.Uuid, bytes))
                        );
                })
            );
            registrations.Add(device
                .WhenyAnyDescriptorDiscovered()
                .Subscribe(desc =>
                {
                    if (flags.HasFlag(BleLogFlags.DescriptorDiscovered))
                        ob.OnNext(new BleLogEvent(BleLogFlags.DescriptorDiscovered, desc.Uuid, String.Empty));
                
                    if (flags.HasFlag(BleLogFlags.DescriptorRead))
                        registrations.Add(desc
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, BleLogFlags.DescriptorRead, desc.Uuid, bytes))
                        );

                    if (flags.HasFlag(BleLogFlags.DescriptorWrite))
                        registrations.Add(desc
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, BleLogFlags.DescriptorWrite, desc.Uuid, bytes))
                        );
                })
            );
        }


        static void Write(IObserver<BleLogEvent> ob, BleLogFlags flag, Guid? uuid, string value)
        {
            var ev = new BleLogEvent(flag, uuid, value);
            Debug.WriteLine($"[{flag}]({uuid}) {value}");
            ob.OnNext(ev);
        }


        static void Write(IObserver<BleLogEvent> ob, BleLogFlags flag, Guid uuid, byte[] bytes)
        {   
            var value = BitConverter.ToString(bytes);
            Debug.WriteLine($"[{flag}]({uuid}) {value}");
            ob.OnNext(new BleLogEvent(flag, uuid, "Value: " + value));
        }
    }
}

