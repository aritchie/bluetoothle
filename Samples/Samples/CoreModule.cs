using System;
using Plugin.BluetoothLE;
using Acr.UserDialogs;
using Autofac;
using Samples.Services.Impl;


namespace Samples
{
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
                .Register(_ => CrossBleAdapter.AdapterScanner)
                .As<IAdapterScanner>()
                .SingleInstance();

            builder
                .Register(_ => CrossBleAdapter.Current)
                .As<IAdapter>()
                .InstancePerDependency();

            builder
                .RegisterType<ViewModelManagerImpl>()
                .AsImplementedInterfaces()
                .SingleInstance();

            //builder
            //    .Register(x => Settings.Current.Bind<AppSettingsImpl>())
            //    .As<IAppSettings>()
            //    .SingleInstance();

            //builder
            //    .Register(x => CrossNotifications.Current)
            //    .As<INotifications>()
            //    .SingleInstance();

            builder
                .RegisterType<AppStateImpl>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .Register(x => UserDialogs.Instance)
                .As<IUserDialogs>()
                .SingleInstance();

            builder
                .RegisterType<CoreServicesImpl>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.Namespace?.StartsWith("Samples.Tasks") ?? false)
                .AsImplementedInterfaces()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.Namespace?.StartsWith("Samples.Pages") ?? false)
                .AsSelf()
                .InstancePerDependency();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.Namespace?.StartsWith("Samples.ViewModels") ?? false)
                .AsSelf()
                .InstancePerDependency();
        }
    }
}
