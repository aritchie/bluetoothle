using System;
using ReactiveUI;
using Samples.ViewModels.Le;
using Xamarin.Forms;


namespace Samples.Pages.Le
{
    public partial class LogPage : Acr.XamForms.ContentPage
    {
        public LogPage(LogViewModel viewModel)
        {
            InitializeComponent();
            this.BindingContext = viewModel;
        }


        IDisposable outputMon;
        protected override void OnAppearing()
        {
            base.OnAppearing();
            var vm = (LogViewModel)this.BindingContext;
            this.outputMon = vm
                .WhenAnyValue(x => x.Log)
                .Subscribe(_ => Device.BeginInvokeOnMainThread(() =>
                    this.scrollView.ScrollToAsync(this.lblOutput, ScrollToPosition.End, true)
                ));
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            this.outputMon.Dispose();
        }
    }
}
