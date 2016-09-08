using System;
using Acr;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class DevicePage : TabbedPage
    {
        public DevicePage()
        {
            InitializeComponent();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            (this.BindingContext as IViewModelLifecycle)?.OnActivate();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (this.BindingContext as IViewModelLifecycle)?.OnDeactivate();
        }
    }
}
