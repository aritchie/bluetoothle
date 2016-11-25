using System;
using System.Linq;
using Acr.Ble;
using Acr.UserDialogs;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Autofac;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;


namespace Samples.Droid
{
    [Activity(
        Label = "Bluetooth Sample",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Forms.Init(this, bundle);
            FormsAppCompatActivity.ToolbarResource = Resource.Layout.toolbar;
            FormsAppCompatActivity.TabLayoutResource = Resource.Layout.tabs;

            AndroidConfig.MaxTransmissionUnitSize = 255;
            UserDialogs.Init(() => (Activity)Forms.Context);

            var builder = new ContainerBuilder();
            builder.RegisterModule(new PlatformModule());
            var container = builder.Build();

            this.LoadApplication(new App(container));

            this.RequestPermissions(new [] { "android.permission.ACCESS_COARSE_LOCATION" }, 0);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
        }
    }
}

