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
        //public static IContainer Container { get; private set; }
        readonly IContainer container;


        public App()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new CoreModule());
            this.container = builder.Build();

            this.MainPage = CrossBleAdapter.AdapterScanner.IsSupported
                ? new NavigationPage(new AdapterListPage
                {
                    BindingContext = this.container.Resolve<AdapterListViewModel>()
                })
                : new NavigationPage(new MainPage
                {
                    BindingContext = this.container.Resolve<MainViewModel>()
                });
        }


        protected override void OnResume()
        {
            base.OnResume();
            var apps = this.container.Resolve<IEnumerable<IAppLifecycle>>();
            foreach (var app in apps)
                app.OnForeground();
        }


        protected override void OnSleep()
        {
            base.OnSleep();
            var apps = this.container.Resolve<IEnumerable<IAppLifecycle>>();
            foreach (var app in apps)
                app.OnBackground();
        }
    }
}