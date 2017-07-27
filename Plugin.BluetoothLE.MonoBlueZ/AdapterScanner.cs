using System;
using System.Reactive;
using System.Reactive.Linq;
using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;


namespace Plugin.BluetoothLE
{
    //https://github.com/brookpatten/Mono.BlueZ/blob/master/Mono.BlueZ.Console/BlendMicroBootstrap.cs
    public class AdapterScanner : IAdapterScanner
    {
        public bool IsSupported => Bus.System?.IsConnected ?? false;


        static IObservable<Unit> dbusLoop;


        public static IObservable<Unit> DBusLoop()
        {
            dbusLoop = dbusLoop ?? Observable.Create<Unit>(ob =>
            {
                var cancel = false;
                while (!cancel)
                    Bus.System.Iterate();

                return () => cancel = true;
            })
            .Publish()
            .RefCount();

            return dbusLoop;
        }


        public IObservable<IAdapter> FindAdapters() => Observable.Create<IAdapter>(ob =>
        {
            var objectManager = Bus.System.GetObject<ObjectManager>(BlueZPath.Service, ObjectPath.Root);
            var agentManager = Bus.System.GetObject<AgentManager1>(BlueZPath.Service, new ObjectPath("/org/bluez"));

            objectManager.InterfacesAdded += (path, i) =>
            {
                ob.OnNext(new Adapter(objectManager, agentManager, path));
            };
            //manager.InterfacesRemoved += (p, i) =>

            var managedObjects = objectManager.GetManagedObjects();
            var dbusName = typeof(LEAdvertisingManager1).DBusInterfaceName();

            foreach (var path in managedObjects.Keys)
            {
                if (managedObjects[path].ContainsKey(dbusName))
                {
                    ob.OnNext(new Adapter(objectManager, agentManager, path));
                }
            }
            return DBusLoop().Subscribe();
        });
    }
}
/*
	string serviceUUID="713d0000-503e-4c75-ba94-3148f18d941e";
	string charVendorName = "713D0001-503E-4C75-BA94-3148F18D941E";
	string charRead = "713D0002-503E-4C75-BA94-3148F18D941E";//rx
	string charWrite = "713D0003-503E-4C75-BA94-3148F18D941E";//tx
	string charAck = "713D0004-503E-4C75-BA94-3148F18D941E";
	string charVersion = "713D0005-503E-4C75-BA94-3148F18D941E";
	string clientCharacteristic = "00002902-0000-1000-8000-00805f9b34fb";


		//get a dbus proxy to the adapter
		var adapter = GetObject<Adapter1> (Service, adapterPath);
		gattManager = GetObject<GattManager1>(Service,adapterPath);
		var gattProfile = new BlendGattProfile();
		_system.Register(gattProfilePath,gattProfile);
		gattManager.RegisterProfile(gattProfilePath,new string[]{charRead},new Dictionary<string,object>());
		System.Console.WriteLine("Registered gatt profile");

		//assume discovery for ble
		//scan for any new devices
		System.Console.WriteLine("Starting LE Discovery...");
		var discoveryProperties = new Dictionary<string,object>();
		discoveryProperties["Transport"]="le";
		adapter.SetDiscoveryFilter(discoveryProperties);
		adapter.StartDiscovery ();
		Thread.Sleep(5000);//totally arbitrary constant, the best kind
		//Thread.Sleep ((int)adapter.DiscoverableTimeout * 1000);

		//refresh the object graph to get any devices that were discovered
		//arguably we should do this in the objectmanager added/removed events and skip the full
		//refresh, but I'm lazy.
		System.Console.WriteLine("Discovery complete, refreshing");
		managedObjects = manager.GetManagedObjects();

		foreach (var obj in managedObjects.Keys) {
			if (obj.ToString ().StartsWith (adapterPath.ToString ())) {
				if (managedObjects [obj].ContainsKey (typeof(Device1).DBusInterfaceName ())) {

					var managedObject = managedObjects [obj];
					if(managedObject[typeof(Device1).DBusInterfaceName()].ContainsKey("Name"))
					{
						var name = (string)managedObject[typeof(Device1).DBusInterfaceName()]["Name"];

						if (name.StartsWith ("MrGibbs"))
						{
							System.Console.WriteLine ("Device " + name + " at " + obj);
							var device = _system.GetObject<Device1> (Service, obj);

							var uuids = device.UUIDs;
							foreach(var uuid in device.UUIDs)
							{
								System.Console.WriteLine("\tUUID: "+uuid);
							}

							devices.Add(device);

						}
					}
				}
			}
		}

		var readCharPath = new ObjectPath("/org/bluez/hci0/dev_F6_58_7F_09_5D_E6/service000c/char000f");
		var  readChar= GetObject<GattCharacteristic1>(Service,readCharPath);
		var properties = GetObject<Properties>(Service,readCharPath);

		properties.PropertiesChanged += new PropertiesChangedHandler(
			new Action<string,IDictionary<string,object>,string[]>((@interface,changed,invalidated)=>{
				System.Console.WriteLine("Properties Changed on "+@interface);
				if(changed!=null)
				{
					foreach(var prop in changed.Keys)
					{
						if(changed[prop] is byte[])
						{
							foreach(var b in ((byte[])changed[prop]))
							{
								System.Console.Write(b+",");
							}
							System.Console.WriteLine("");
						}
						else
						{
							System.Console.WriteLine("{0}={1}",prop,changed[prop]);
						}
					}
				}

				if(invalidated!=null)
				{
					foreach(var prop in invalidated)
					{
						System.Console.WriteLine(prop+" Invalidated");
					}
				}
			}));

		foreach(var device in devices)
		{
			System.Console.WriteLine("Connecting to "+device.Name);
			device.Connect();
			System.Console.WriteLine("\tConnected");
		}

		readChar.StartNotify();

		System.Threading.Thread.Sleep(10000);

		readChar.StopNotify();
		System.Threading.Thread.Sleep(500);
	}
	finally
	{
		if (devices != null) {
			foreach(var device in devices)
			{
				System.Console.WriteLine("Disconnecting "+device.Name);
				device.Disconnect();
				System.Console.WriteLine("\tDisconnected");
			}
		}
		agentManager.UnregisterAgent (agentPath);
		gattManager.UnregisterProfile (gattProfilePath);
	}
}


public static byte[] CombineArrays( params byte[][] array )
{
	var rv = new byte[array.Select( x => x.Length ).Sum()];

	for ( int i = 0, insertionPoint = 0; i < array.Length; insertionPoint += array[i].Length, i++ )
		Array.Copy( array[i], 0, rv, insertionPoint, array[i].Length );
	return rv;
}


public class BlendGattProfile:GattProfile1
{
public BlendGattProfile()
{
}

public void Release()
{
	System.Console.WriteLine ("GattProfile1.Release");
}

}
*/