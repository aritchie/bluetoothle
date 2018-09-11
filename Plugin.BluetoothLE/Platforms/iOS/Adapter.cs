using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Plugin.BluetoothLE.Server;
using UIKit;
using Foundation;


namespace Plugin.BluetoothLE
{
    public partial class Adapter : AbstractAdapter
    {
        public Adapter(BleAdapterConfiguration config = null)
        {
            this.context = new AdapterContext(config);
            this.Advertiser = new Advertiser(this.context.PeripheralManager);
        }


        public override AdapterFeatures Features
        {
            get
            {
                var v8or9 = UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);
                return v8or9
                    ? AdapterFeatures.AllServer | AdapterFeatures.OpenSettings
                    : AdapterFeatures.AllServer;
            }
        }


        public override IObservable<IGattServer> CreateGattServer() => Observable.FromAsync(async ct =>
        {
            var cb = this.context.PeripheralManager;
            if (cb.State != CBPeripheralManagerState.PoweredOn)
            {
                await Task.Delay(3000).ConfigureAwait(false);
                if (cb.State != CBPeripheralManagerState.PoweredOn)
                    throw new BleException("Invalid Adapter State - " + cb.State);
            }

            return new GattServer(cb);
        });

        public override void OpenSettings()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                //var workSpaceClassId = Class.GetHandle("LSApplicationWorkspace");
                //if (workSpaceClassId != IntPtr.Zero)
                //{
                //    var workSpaceClass = new NSObject(workSpaceClassId);
                //    var workSpaceInstance = workSpaceClass.PerformSelector(new Selector("defaultWorkspace"));

                //    var selector = new Selector("openSensitiveURL:withOptions:");
                //    if (workSpaceInstance.RespondsToSelector(selector))
                //    {
                //        workSpaceInstance.PerformSelector(selector, new NSUrl("Prefs:root=Bluetooth"));
                //    }
                //}
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                OpenUrl("prefs:root=Bluetooth");
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                OpenUrl("prefs:root=General&path=Bluetooth");
            }
        }


        static void OpenUrl(string url)
        {
            var nsurl = new NSUrl(url);
            if (UIApplication.SharedApplication.CanOpenUrl(nsurl))
                UIApplication.SharedApplication.OpenUrl(nsurl);
        }
	}
}