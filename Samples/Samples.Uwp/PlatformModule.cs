using System;
using Autofac;
using SQLite.Net.Platform.WinRT;


namespace Samples.Uwp
{
    public class PlatformModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterModule(new CoreModule());
            builder
                .Register(x => new SampleDbConnection(
                    new SQLitePlatformWinRT(),
                    Windows.Storage.ApplicationData.Current.LocalFolder.Path
                ))
                .AsSelf()
                .SingleInstance();
        }
    }
}
