using System;
using Acr.XamForms;
using Autofac;
using Samples.ViewModels.Le;


namespace Samples.Pages.Le
{
    public partial class ScanPage : ContentPage
    {
        public ScanPage()
        {
            this.InitializeComponent();
            this.BindingContext = App.Container.Resolve<ScanViewModel>();
        }
    }
}
