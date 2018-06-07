using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;


namespace Plugin.BluetoothLE.Internals
{
    public class DeviceManager
    {
        readonly ConcurrentDictionary<string, IDevice> devices = new ConcurrentDictionary<string, IDevice>();
        readonly BluetoothManager manager;
        readonly List<ConnectionStatus> nonconnectedstates = new List<ConnectionStatus>() { ConnectionStatus.Connecting, ConnectionStatus.Disconnected, ConnectionStatus.Disconnecting };


        public DeviceManager(BluetoothManager manager)
        {
            this.manager = manager;
        }


        public IDevice GetDevice(BluetoothDevice btDevice) => this.devices.GetOrAdd(
            btDevice.Address,
            x => new Device(this.manager, btDevice)
        );

        public List<IDevice> GetDevicesWithConnectionState(ConnectionStatus state)
        {
            int[] btstates = new int[1];
            switch (state)
            {
                case ConnectionStatus.Disconnected: btstates[0] = (int)ProfileState.Disconnected; break;
                case ConnectionStatus.Disconnecting: btstates[0] = (int)ProfileState.Disconnecting; break;
                case ConnectionStatus.Connecting: btstates[0] = (int)ProfileState.Connecting; break;
                case ConnectionStatus.Connected: btstates[0] = (int)ProfileState.Connected; break;
            }
            IList<BluetoothDevice> devs = manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, btstates);
            List<IDevice> res = new List<IDevice>();
            foreach (BluetoothDevice d in devs)
            {
                IDevice id = GetDevice(d); if (id != null) res.Add(id);
            }
            return res;
        }
        public List<IDevice> GetDevicesWithConnectionStates(List<ConnectionStatus> states)
        {
            int statecount = states.Count;
            int[] btstates = new int[statecount];            
            for (int i=0;i< statecount;++i)
            {
                switch(states[i])
                {
                    case ConnectionStatus.Disconnected: btstates[i] = (int)ProfileState.Disconnected;break;
                    case ConnectionStatus.Disconnecting: btstates[i] = (int)ProfileState.Disconnecting; break;
                    case ConnectionStatus.Connecting: btstates[i] = (int)ProfileState.Connecting; break;
                    case ConnectionStatus.Connected: btstates[i] = (int)ProfileState.Connected; break;
                }
            }
            IList<BluetoothDevice> devs= manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, btstates);
            List<IDevice> res = new List<IDevice>();
            foreach (BluetoothDevice d in devs)
            {
                IDevice id=GetDevice(d);if (id != null) res.Add(id);
            }
            return res;
        }
        private List<string> GetDeviceAddressesWithConnectionStates(List<ConnectionStatus> states)
        {
            int statecount = states.Count;
            int[] btstates = new int[statecount];
            for (int i = 0; i < statecount; ++i)
            {
                switch (states[i])
                {
                    case ConnectionStatus.Disconnected: btstates[i] = (int)ProfileState.Disconnected; break;
                    case ConnectionStatus.Disconnecting: btstates[i] = (int)ProfileState.Disconnecting; break;
                    case ConnectionStatus.Connecting: btstates[i] = (int)ProfileState.Connecting; break;
                    case ConnectionStatus.Connected: btstates[i] = (int)ProfileState.Connected; break;
                }
            }
            IList<BluetoothDevice> devs = manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, btstates);
            List<string> res = new List<string>();
            foreach (BluetoothDevice d in devs) res.Add(d.Address);
            return res;
        }

        public IEnumerable<IDevice> GetConnectedDevices() => GetDevicesWithConnectionState(ConnectionStatus.Connected);
        
        public void Clear() => GetDeviceAddressesWithConnectionStates(nonconnectedstates)
            .ForEach(x => this.devices.TryRemove(x, out _));
    }
}