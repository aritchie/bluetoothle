using System;
using Autofac;
using Samples.ViewModels.Le;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class ServerPage : ContentPage
    {
        public ServerPage()
        {
            this.InitializeComponent();
            this.BindingContext = App.Container.Resolve<ServerViewModel>();
        }
    }
}
