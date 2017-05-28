using System;
using Autofac;
using Samples.ViewModels.Le;


namespace Samples.Pages.Le
{
    public partial class AdapterListPage : Samples.Pages.ContentPage
    {
        public AdapterListPage()
        {
            this.InitializeComponent();
            this.BindingContext = App.Container.Resolve<AdapterListViewModel>();
        }
    }
}