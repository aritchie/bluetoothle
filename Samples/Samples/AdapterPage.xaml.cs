using System;
using Prism.Common;
using Prism.Navigation;
using Xamarin.Forms;


namespace Samples
{
    public partial class AdapterPage : TabbedPage, INavigatingAware
    {
        public AdapterPage ()
        {
            this.InitializeComponent();
        }


        public void OnNavigatingTo(NavigationParameters parameters)
        {
            foreach (var child in this.Children)
            {
                PageUtilities.OnNavigatingTo(child, parameters);
            }
        }
    }
}