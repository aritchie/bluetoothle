using System;
using Autofac;
using SQLite.Net.Platform.XamarinIOS;


namespace Samples.iOS
{
    public class PlatformModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterModule(new CoreModule());
            builder
                .Register(x => new SampleDbConnection(
                    new SQLitePlatformIOS(),
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal)
                ))
                .AsSelf()
                .SingleInstance();
        }
    }
}
