using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DBus;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class Adapter : AbstractAdapter
    {
        readonly Adapter1 native;
        readonly GattManager1 gattManager;

        readonly AgentManager1 agentManager;
        readonly ObjectPath path;
        readonly AcrAgent agent;
        readonly Subject<bool> scanStatusSubj;

        public Adapter(AgentManager1 agentManger, ObjectPath path)
        {
            this.native = Bus.System.GetObject<Adapter1>(Constants.SERVICE, path);
            this.gattManager = Bus.System.GetObject<GattManager1>(Constants.SERVICE, path);

            //Bus.System.Register(agentPath, agent);
            //agentManager.RequestDefaultAgent(Constants.AgentPath);
            //var agentManager = GetObject<AgentManager1> (Service, blueZPath);
            //var agent = new DemoAgent ();
            //GattManager1 gattManager=null;
            //register our agent and make it the default
            //_system.Register (agentPath, agent);
            //agentManager.RequestDefaultAgent (agentPath);


            //var gattProfile = new BlendGattProfile();
            //_system.Register(gattProfilePath,gattProfile);
            //gattManager.RegisterProfile(gattProfilePath,new string[]{charRead},new Dictionary<string,object>());
            //System.Console.WriteLine("Registered gatt profile");

            this.agentManager = agentManger;
            this.path = path;
            this.scanStatusSubj = new Subject<bool>();
        }


        public override bool IsScanning => this.native.Discovering;


        public override IObservable<bool> WhenScanningStatusChanged()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IScanResult> Scan(ScanConfig config = null)
        {
            throw new NotImplementedException();
        }


        public override IObservable<IScanResult> ScanListen() => Observable.Create<IScanResult>(ob =>
        {
            this.native.SetDiscoveryFilter(new Dictionary<string, object> {{"Transport", "le" }});
            this.native.StartDiscovery();
            this.scanStatusSubj.OnNext(true);

            //managedObjects = objectManager.GetManagedObjects();

            //foreach (var obj in managedObjects.Keys) {
            //    if (obj.ToString ().StartsWith (adapterPath.ToString ())) {
            //        if (managedObjects [obj].ContainsKey (typeof(Device1).DBusInterfaceName ())) {

            //            var managedObject = managedObjects [obj];
            //            if(managedObject[typeof(Device1).DBusInterfaceName()].ContainsKey("Name"))
            //            {
            //                var name = (string)managedObject[typeof(Device1).DBusInterfaceName()]["Name"];

            //                if (name.StartsWith ("MrGibbs"))
            //                {
            //                    System.Console.WriteLine ("Device " + name + " at " + obj);
            //                    var device = _system.GetObject<Device1> (Service, obj);

            //                    var uuids = device.UUIDs;
            //                    foreach(var uuid in device.UUIDs)
            //                    {
            //                        System.Console.WriteLine("\tUUID: "+uuid);
            //                    }

            //                    devices.Add(device);

            //                }
            //            }
            //        }
            //    }
            //}
            return () =>
            {
                this.scanStatusSubj.OnNext(false);
                this.native.StopDiscovery();
            };
        })
        .Publish()
        .RefCount();


        public override IObservable<AdapterStatus> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}
