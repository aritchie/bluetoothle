using System;
using Samples.ViewModels;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class DevicePage : TabbedPage
    {
        public DevicePage()
        {
            this.InitializeComponent();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            (this.BindingContext as IViewModel)?.OnActivate();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (this.BindingContext as IViewModel)?.OnDeactivate();
        }
    }
}
