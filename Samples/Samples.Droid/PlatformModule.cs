using System;
using Autofac;


namespace Samples.Droid
{
    public class PlatformModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterModule(new CoreModule());
            builder
                .Register(x => new SampleDbConnection(Environment.GetFolderPath(Environment.SpecialFolder.Personal)))
                .AsSelf()
                .SingleInstance();
        }
    }
}
