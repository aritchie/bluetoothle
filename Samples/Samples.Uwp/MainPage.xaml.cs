using System;
using Autofac;


namespace Samples.Uwp
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new PlatformModule());
            var container = builder.Build();
            this.LoadApplication(new Samples.App(container));
        }
    }
}
