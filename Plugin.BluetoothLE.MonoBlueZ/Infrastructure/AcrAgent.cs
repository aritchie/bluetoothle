using System;
using DBus;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    public class AcrAgent : Agent1
    {
        public void Release()
        {
        }

        public string RequestPinCode(ObjectPath device)
        {
            throw new NotImplementedException();
        }


        public void DisplayPinCode(ObjectPath device, string pinCode)
        {
        }


        public uint RequestPasskey(ObjectPath device) => 1;


        public void DisplayPasskey(ObjectPath device, uint passkey, ushort entered)
        {
        }


        public void RequestConfirmation(ObjectPath device, uint passkey)
        {
        }


        public void RequestAuthorization(ObjectPath device)
        {
        }


        public void AuthorizeService(ObjectPath device, string uuid)
        {
        }


        public void Cancel()
        {
        }
    }
}
/*
public DemoAgent ()
		{
		}
		public void Release()
		{
			System.Console.WriteLine ("Release");
		}
		public string RequestPinCode(ObjectPath device)
		{
			return "1";
		}
		public void DisplayPinCode(ObjectPath device,string pinCode)
		{
			System.Console.WriteLine ("DisplayPinCode");
		}
		public uint RequestPasskey(ObjectPath device)
		{
			return 1;
		}
		public void DisplayPasskey (ObjectPath device, uint passkey, ushort entered)
		{
			System.Console.WriteLine ("DisplayPasskey");
		}
		public void RequestConfirmation(ObjectPath device,uint passkey)
		{
			System.Console.WriteLine ("RequestConfirmation");
		}
		public void RequestAuthorization(ObjectPath device)
		{
			System.Console.WriteLine ("RequestAuthorization");
		}
		public void AuthorizeService(ObjectPath device,string uuid)
		{
			System.Console.WriteLine ("AuthorizeService");
		}
		public void Cancel()
		{
		}
     */