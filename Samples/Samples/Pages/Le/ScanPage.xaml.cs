using System;
using Acr.XamForms;


namespace Samples.Pages.Le
{

    public partial class ScanPage : ContentPage
    {

        public ScanPage()
        {
            this.InitializeComponent();
            this.SearchBar.SearchButtonPressed += (sender, args) =>
            {
                this.SearchBar.Unfocus();
            };
        }
    }
}
