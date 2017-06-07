using System;
using Samples.ViewModels;


namespace Samples.Pages
{
    public class ContentPage : Xamarin.Forms.ContentPage
    {
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
