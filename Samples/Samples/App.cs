using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            if (CrossBleAdapter.AdapterScanner == null)
            {
                var page = container.Resolve<MainPage>();
                this.MainPage = new NavigationPage(page);
            }
            else
            {
                this.MainPage = new NavigationPage(new AdapterListPage());
            }

            Plugin.BluetoothLE.Log.Out = x => Debug.WriteLine(x);
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