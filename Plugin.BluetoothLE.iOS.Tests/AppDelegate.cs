using System;
using System.Reflection;
using Foundation;
using Plugin.BluetoothLE.Tests;
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
            this.AddTestAssembly(typeof(AdapterTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            this.AutoStart = true;
            this.TerminateAfterExecution = false;

            return base.FinishedLaunching(app, options);
        }
    }
}