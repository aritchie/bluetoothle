using System;
using System.Reflection;
using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Xamarin.Forms;
using Xunit;
using Xunit.Runners.UI;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Plugin.BluetoothLE.Android.Tests
{
    [Activity(
        Label = "BLE Plugin Tests",
        MainLauncher = true
    )]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            //GattConnectionConfig.DefaultConfiguration.AutoConnect = false;

            this.RequestPermissions(new[]
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.BluetoothPrivileged
            }, 0);

            UserDialogs.Init(() => (Activity)Forms.Context);
            this.AddTestAssembly(typeof(BluetoothLE.Tests.DeviceTests).Assembly);
            this.AddTestAssembly(Assembly.GetExecutingAssembly());

            //CrossBleAdapter.UseNewScanner = false;
            //CrossBleAdapter.PauseBeforeServiceDiscovery = TimeSpan.FromSeconds(1);
            //CrossBleAdapter.PauseBetweenInvocations = TimeSpan.FromMilliseconds(250);
            //CrossBleAdapter.ShouldInvokeOnMainThread = false;

            this.AutoStart = false;
            this.TerminateAfterExecution = false;

            base.OnCreate(bundle);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
        }
    }
}

