using System;
using Autofac;
using Samples.ViewModels.Le;


namespace Samples.Pages.Le
{
    public partial class ConnectedDevicesPage : Acr.XamForms.ContentPage
    {
        public ConnectedDevicesPage()
        {
            InitializeComponent();
            this.BindingContext = App.Container.Resolve<ConnectedDevicesViewModel>();
        }
    }
}
