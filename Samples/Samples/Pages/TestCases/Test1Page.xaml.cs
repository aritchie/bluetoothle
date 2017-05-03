using System;
using Autofac;
using Samples.ViewModels.TestCases;
using Xamarin.Forms;


namespace Samples.Pages.TestCases
{
    public partial class Test1Page : ContentPage
    {
        public Test1Page()
        {
            this.InitializeComponent();
            this.BindingContext = App.Container.Resolve<Test1ViewModel>();
        }
    }
}
