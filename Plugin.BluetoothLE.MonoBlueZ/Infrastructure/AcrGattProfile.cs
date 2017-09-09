using System;
using System.Collections.Generic;
using DBus;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    public class AcrGattProfile : Profile1
    {
        public void Release()
        {
            throw new NotImplementedException();
        }


        public void NewConnection(ObjectPath device, FileDescriptor fd, IDictionary<string, object> properties)
        {
            throw new NotImplementedException();
        }


        public void RequestDisconnection(ObjectPath device)
        {
            throw new NotImplementedException();
        }
    }
}
