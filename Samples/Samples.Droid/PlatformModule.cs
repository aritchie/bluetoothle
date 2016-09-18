using System;
using Autofac;
using SQLite.Net.Platform.XamarinAndroid;


namespace Samples.Droid
{
    public class PlatformModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterModule(new CoreModule());
            builder
                .Register(x => new SampleDbConnection(
                    new SQLitePlatformAndroid(),
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal)
                ))
                .AsSelf()
                .SingleInstance();
        }
    }
}
