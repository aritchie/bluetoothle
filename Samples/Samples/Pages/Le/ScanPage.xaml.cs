using System;
using Acr.XamForms;
using Samples.ViewModels.Le;


namespace Samples.Pages.Le
{

    public partial class ScanPage : ContentPage
    {
        public ScanPage(ScanViewModel viewModel)
        {
            this.InitializeComponent();
            this.BindingContext = viewModel;
            this.SearchBar.SearchButtonPressed += (sender, args) =>
            {
                this.SearchBar.Unfocus();
            };
        }
    }
}
