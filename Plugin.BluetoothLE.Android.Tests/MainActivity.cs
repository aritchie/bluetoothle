using System;
using System.Reflection;
using Android.App;
using Android.OS;
using Plugin.BluetoothLE.Tests;
using Xunit.Runners.UI;
using Xunit.Sdk;


namespace Plugin.BluetoothLE.Android.Tests
{
    [Activity(
        Label = "Plugin.BluetoothLE.Android.Tests",
        MainLauncher = true,
        Icon = "@drawable/icon"
    )]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            this.AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            this.AddTestAssembly(typeof(SpecificTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            this.AutoStart = true;
            this.TerminateAfterExecution = false;
            //this.Writer =

            base.OnCreate(bundle);
        }
    }
}

