using System;
using Autofac;
using Samples.ViewModels.Le;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class BackgroundPage : ContentPage
    {
        public BackgroundPage()
        {
            InitializeComponent();
            this.BindingContext = App.Container.Resolve<BackgroundViewModel>();
        }
    }
}
