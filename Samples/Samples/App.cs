using System;
using System.Collections.Generic;
using Acr;
using Autofac;
using Plugin.BluetoothLE;
using Samples.Pages;
using Samples.Pages.Le;
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

            this.MainPage = CrossBleAdapter.AdapterScanner.IsSupported
                ? new NavigationPage(new AdapterListPage())
                : new NavigationPage(new MainPage());
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