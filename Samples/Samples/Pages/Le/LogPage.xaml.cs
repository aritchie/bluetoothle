using System;
using Autofac;
using ReactiveUI;
using Samples.ViewModels.Le;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class LogPage : Acr.XamForms.ContentPage
    {
        public LogPage()
        {
            InitializeComponent();
            this.BindingContext = App.Container.Resolve<LogViewModel>();
        }
    }
}
