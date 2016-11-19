using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;


namespace Acr.Ble.Plugins
{
    public static class Logging
    {
        public static IObservable<BleLogEvent> CreateLogger(this IAdapter adapter, BleLogFlags flags = BleLogFlags.AdapterStatus | BleLogFlags.DeviceStatus)
        {
            return Observable.Create<BleLogEvent>(ob =>
            {
                var list = new List<IDisposable>();
                var deviceEvents = new Dictionary<Guid, List<IDisposable>>();

                if (flags.HasFlag(BleLogFlags.AdapterStatus))
                {
                    list.Add(adapter
                        .WhenStatusChanged()
                        .Subscribe(status => Write(ob, null, BleLogFlags.AdapterStatus, null, $"Changed to {status}"))
                    );
                }
                if (flags.HasFlag(BleLogFlags.AdapterScanResults))
                {
                    list.Add(adapter
                        .ScanListen()
                        .Subscribe(scanResult => Write(ob, null, BleLogFlags.AdapterScanResults, null, $"Device: {scanResult.Device.Uuid} - RSSI: {scanResult.Rssi}"))
                    );
                }
                if (flags.HasFlag(BleLogFlags.AdapterScanStatus))
                {
                    list.Add(adapter
                        .WhenScanningStatusChanged()
                        .Subscribe(status => Write(ob, null, BleLogFlags.AdapterScanStatus, null, $"Changed to {status}"))
                    );
                }

                list.Add(adapter
                    .WhenDeviceStatusChanged()
                    .Where(x => 
                        x.Status == ConnectionStatus.Connected || 
                        x.Status == ConnectionStatus.Disconnected
                    )
                    .Subscribe(device =>
                    {
                        if (device.Status == ConnectionStatus.Connected)
                        {
                            if (flags.HasFlag(BleLogFlags.DeviceConnected))
                                Write(ob, device, BleLogFlags.DeviceConnected, device.Uuid, $"Changed to {device.Status}");

                            HookDevice(device, ob, deviceEvents, flags);
                        }
                        else if (flags.HasFlag(BleLogFlags.DeviceDisconnected))
                            Write(ob, device, BleLogFlags.DeviceDisconnected, device.Uuid, $"Changed to {device.Status}");
                    }));

                foreach (var device in adapter.GetConnectedDevices())
                {
                    HookDevice(device, ob, deviceEvents, flags);
                }

                return () =>
                {
                    foreach (var deviceList in deviceEvents.Values)
                        foreach (var device in deviceList)
                            device.Dispose();

                    foreach (var dispose in list)
                        dispose.Dispose();
                };
            });
        }


        public static IObservable<BleLogEvent> CreateLogger(this IDevice device, BleLogFlags flags = BleLogFlags.DeviceStatus | BleLogFlags.CharacteristicAll)
        {
            return Observable.Create<BleLogEvent>(ob =>
            {
                var list = new List<IDisposable>();

                var deviceOb = device
                    .WhenStatusChanged()
                    .Where(x => 
                        x == ConnectionStatus.Connected || 
                        x == ConnectionStatus.Disconnected
                    )
                    .Subscribe(status =>
                    {
                        if (device.Status == ConnectionStatus.Connected)
                        {
                            if (flags.HasFlag(BleLogFlags.DeviceConnected))
                                Write(ob, device, BleLogFlags.DeviceConnected, device.Uuid, $"Changed to {device.Status}");

                            HookDeviceEvents(list, device, ob, flags);
                        }
                        else if (flags.HasFlag(BleLogFlags.DeviceDisconnected))
                            Write(ob, device, BleLogFlags.DeviceDisconnected, device.Uuid, $"Changed to {device.Status}");
                    });

                return () =>
                {
                    deviceOb.Dispose();
                    foreach (var item in list)
                        item.Dispose();
                };
            });
        }

        static void HookDevice(IDevice device, IObserver<BleLogEvent> ob, IDictionary<Guid, List<IDisposable>> deviceEvents, BleLogFlags flags)
        {
            lock(deviceEvents)
            {
                switch (device.Status)
                {
                    case ConnectionStatus.Connected:
                        CleanDeviceEvents(deviceEvents, device.Uuid);
                        var reg = new List<IDisposable>();
                        HookDeviceEvents(reg, device, ob, flags);
                        deviceEvents.Add(device.Uuid, reg);
                        break;

                    default:
                        CleanDeviceEvents(deviceEvents, device.Uuid);
                        break;
                }
            }
        }


        static void CleanDeviceEvents(IDictionary<Guid, List<IDisposable>> deviceEvents, Guid deviceId)
        {
            if (!deviceEvents.ContainsKey(deviceId))
                return;

            var registration = deviceEvents[deviceId];
            foreach (var item in registration)
                item.Dispose();

            deviceEvents.Remove(deviceId);
        }


        static void HookDeviceEvents(IList<IDisposable> registrations, IDevice device, IObserver<BleLogEvent> ob, BleLogFlags flags)
        {
            if (flags.HasFlag(BleLogFlags.ServiceDiscovered))
            {
                registrations.Add(device
                    .WhenServiceDiscovered()
                    .Subscribe(serv => Write(ob, device, BleLogFlags.ServiceDiscovered, serv.Uuid, String.Empty))
                );
            }
            registrations.Add(device
                .WhenAnyCharacteristicDiscovered()
                .Subscribe(ch =>
                {
                    if (flags.HasFlag(BleLogFlags.CharacteristicDiscovered))
                        Write(ob, device, BleLogFlags.CharacteristicDiscovered, ch.Uuid, String.Empty);

                    if (flags.HasFlag(BleLogFlags.CharacteristicRead))
                        registrations.Add(ch
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, device, BleLogFlags.CharacteristicRead, ch.Uuid, bytes))
                        );

                    if (flags.HasFlag(BleLogFlags.CharacteristicWrite))
                        registrations.Add(ch
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, device, BleLogFlags.CharacteristicWrite, ch.Uuid, bytes))
                        );

                    if (flags.HasFlag(BleLogFlags.CharacteristicNotify) && ch.CanNotify())
                        registrations.Add(ch
                            .WhenNotificationReceived()
                            .Subscribe(bytes => Write(ob, device, BleLogFlags.CharacteristicNotify, ch.Uuid, bytes))
                        );
                })
            );
            registrations.Add(device
                .WhenyAnyDescriptorDiscovered()
                .Subscribe(desc =>
                {
                    if (flags.HasFlag(BleLogFlags.DescriptorDiscovered))
                        Write(ob, device, BleLogFlags.DescriptorDiscovered, desc.Uuid, String.Empty);

                    if (flags.HasFlag(BleLogFlags.DescriptorRead))
                        registrations.Add(desc
                            .WhenRead()
                            .Subscribe(bytes => Write(ob, device, BleLogFlags.DescriptorRead, desc.Uuid, bytes))
                        );

                    if (flags.HasFlag(BleLogFlags.DescriptorWrite))
                        registrations.Add(desc
                            .WhenWritten()
                            .Subscribe(bytes => Write(ob, device, BleLogFlags.DescriptorWrite, desc.Uuid, bytes))
                        );
                })
            );
        }


        static void Write(IObserver<BleLogEvent> ob, IDevice device, BleLogFlags flag, Guid? uuid, string value)
        {
            var ev = new BleLogEvent(device, flag, uuid, null, value);
            ob.OnNext(ev);
#if DEBUG
            Debug.WriteLine($"[{flag}]({uuid}) {value}");
#endif
        }


        static void Write(IObserver<BleLogEvent> ob, IDevice device, BleLogFlags flag, Guid uuid, byte[] data)
        {
            ob.OnNext(new BleLogEvent(device, flag, uuid, data, null));
#if DEBUG
            var dataString = data == null ? String.Empty : BitConverter.ToString(data);
            Debug.WriteLine($"[{flag}]({uuid}) {dataString}");
#endif
        }
    }
}