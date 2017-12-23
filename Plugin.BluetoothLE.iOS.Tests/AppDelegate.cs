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
            this.AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            this.AddTestAssembly(typeof(BluetoothLE.Tests.DeviceTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            this.AutoStart = false;
            this.TerminateAfterExecution = false;

            return base.FinishedLaunching(app, options);
        }
    }
}