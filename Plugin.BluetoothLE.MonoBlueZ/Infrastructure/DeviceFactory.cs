using System;
using System.Collections.Generic;
using DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    public class DeviceFactory
    {
        readonly object syncLock = new object();
        readonly IDictionary<ObjectPath, IDevice> devices = new Dictionary<ObjectPath, IDevice>();


        public IDevice TryGetOrAdd(ObjectPath path)
        {
            return null;
        }


        public void Clear()
        {

        }
    }
}
