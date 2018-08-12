using System;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using Prism;
using Prism.Autofac;
using Prism.Ioc;
using Prism.Mvvm;
using Samples.Infrastructure;
using Xamarin.Forms;


namespace Samples
{
    public class App : PrismApplication
    {
        public App() : this(null) { }
        public App(IPlatformInitializer initializer) : base(initializer) { }


        protected override async void OnInitialized()
        {
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewModelTypeName = viewType.FullName.Replace("Page", "ViewModel");
                var viewModelType = Type.GetType(viewModelTypeName);
                return viewModelType;
            });
            await this.NavigationService.NavigateAsync($"NavigationPage/AdapterListPage");
        }


        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<IUserDialogs>(UserDialogs.Instance);
            containerRegistry.RegisterInstance<IAdapterScanner>(CrossBleAdapter.AdapterScanner);
            containerRegistry.RegisterSingleton<ILogService, LogService>();

            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<AdapterPage>();

            containerRegistry.RegisterForNavigation<AdapterListPage>();

            containerRegistry.RegisterForNavigation<ScanPage>();
            containerRegistry.RegisterForNavigation<LogPage>();
            containerRegistry.RegisterForNavigation<ServerPage>();

            containerRegistry.RegisterForNavigation<DevicePage>();
        }
    }
}