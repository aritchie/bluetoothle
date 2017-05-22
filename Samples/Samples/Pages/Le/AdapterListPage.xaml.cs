using System;
using Acr.XamForms;
using Autofac;
using Samples.ViewModels.Le;


namespace Samples.Pages.Le
{
    public partial class AdapterListPage : ContentPage
    {
        public AdapterListPage()
        {
            this.InitializeComponent();
            this.BindingContext = App.Container.Resolve<AdapterListViewModel>();
        }
    }
}