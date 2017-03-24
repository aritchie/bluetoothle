using System;
using System.Collections.Generic;
using System.Diagnostics;
using Acr;
using Autofac;
using Samples.Pages;
using Samples.Services;
using Xamarin.Forms;


namespace Samples
{
    public class App : Application
    {
        public static IContainer Container { get; private set; }


        public App(IContainer container)
        {
            Container = container;
            var page = container.Resolve<MainPage>();
            this.MainPage = new NavigationPage(page);

            Plugin.BluetoothLE.Log.Out = x =>
            {
                Debug.WriteLine(x);
            };
        }


        protected override void OnResume()
        {
            base.OnResume();
            Container.Resolve<IEnumerable<IAppLifecycle>>().Each(x => x.OnForeground());
        }


        protected override void OnSleep()
        {
            base.OnSleep();
            Container.Resolve<IEnumerable<IAppLifecycle>>().Each(x => x.OnBackground());
        }
    }
}