﻿using System;
using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Autofac;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;


namespace Samples.Droid
{
    [Activity(
        Label = "ACR BluetoothLE",
        Icon = "@drawable/icon",
        MainLauncher = true,
        Theme = "@style/MainTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
    )]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Forms.Init(this, bundle);
            FormsAppCompatActivity.ToolbarResource = Resource.Layout.Toolbar;
            FormsAppCompatActivity.TabLayoutResource = Resource.Layout.Tabbar;

            UserDialogs.Init(() => (Activity)Forms.Context);

            var builder = new ContainerBuilder();
            builder.RegisterModule(new PlatformModule());
            var container = builder.Build();

            this.LoadApplication(new App(container));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                this.RequestPermissions(new []
                {
                    Manifest.Permission.AccessCoarseLocation,
                    Manifest.Permission.BluetoothPrivileged
                }, 0);
            }
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
        }
    }
}

