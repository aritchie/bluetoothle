using System;
using Autofac;


namespace Samples.Uwp
{
    public class PlatformModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterModule(new CoreModule());
            builder
                .Register(x => new SampleDbConnection(Windows.Storage.ApplicationData.Current.LocalFolder.Path))
                .AsSelf()
                .SingleInstance();
        }
    }
}
