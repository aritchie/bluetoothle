using System;
using System.Collections.Generic;
using Autofac;
using Plugin.BluetoothLE;
using Samples.Pages;
using Samples.Pages.Le;
using Samples.Services;
using Samples.ViewModels;
using Samples.ViewModels.Le;
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
                ? new NavigationPage(new AdapterListPage
                {
                    BindingContext = container.Resolve<AdapterListViewModel>()
                })
                : new NavigationPage(new MainPage
                {
                    BindingContext = container.Resolve<MainViewModel>()
                });
        }


        protected override void OnResume()
        {
            base.OnResume();
            var apps = Container.Resolve<IEnumerable<IAppLifecycle>>();
            foreach (var app in apps)
                app.OnForeground();
        }


        protected override void OnSleep()
        {
            base.OnSleep();
            var apps = Container.Resolve<IEnumerable<IAppLifecycle>>();
            foreach (var app in apps)
                app.OnBackground();
        }
    }
}