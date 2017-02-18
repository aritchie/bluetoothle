using System;
using Plugin.BluetoothLE;
using Acr.Notifications;
using Acr.Settings;
using Acr.UserDialogs;
using Autofac;
using Samples.Services;
using Samples.Services.Impl;


namespace Samples
{
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
                .Register(x => CrossBleAdapter.Current)
                .As<IAdapter>()
                .SingleInstance();

            builder
                .RegisterType<ViewModelManagerImpl>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .Register(x => Settings.Local.Bind<AppSettingsImpl>())
                .As<IAppSettings>()
                .SingleInstance();

            builder
                .Register(x => Notifications.Instance)
                .As<INotifications>()
                .SingleInstance();

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
                .Where(x => x.Namespace.StartsWith("Samples.Tasks"))
                .AsImplementedInterfaces()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.Namespace.StartsWith("Samples.Pages"))
                .AsSelf()
                .InstancePerDependency();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.Namespace.StartsWith("Samples.ViewModels"))
                .AsSelf()
                .InstancePerDependency();
        }
    }
}
