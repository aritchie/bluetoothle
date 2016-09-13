using System;
using Samples.ViewModels.Le;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class BackgroundPage : ContentPage
    {
        public BackgroundPage(BackgroundViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }
    }
}
