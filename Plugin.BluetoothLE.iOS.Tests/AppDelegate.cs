using System;
using System.Reflection;
using Foundation;
using UIKit;
using Xunit.Runner;
using Xunit.Sdk;


namespace Plugin.BluetoothLE.iOS.Tests
{
    [Register("AppDelegate")]
    public partial class AppDelegate : RunnerAppDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Acr.Logging.Log.ToDebug();

            this.AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            this.AddTestAssembly(typeof(Plugin.BluetoothLE.Tests.DeviceTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            this.AutoStart = false;
            this.TerminateAfterExecution = false;
            //[assembly: CollectionBehavior(MaxParallelThreads = n)]

            return base.FinishedLaunching(app, options);
        }
    }
}