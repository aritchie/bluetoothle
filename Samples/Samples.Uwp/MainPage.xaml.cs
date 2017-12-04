using System;
using Autofac;


namespace Samples.Uwp
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.LoadApplication(new Samples.App());
        }
    }
}
