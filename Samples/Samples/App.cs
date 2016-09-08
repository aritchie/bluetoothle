using System;
using System.Collections.Generic;
using Acr;
using Autofac;
using Samples.Services;
using Samples.ViewModels;
using Xamarin.Forms;


namespace Samples
{
    public class App : Application
    {
        readonly IContainer container;


        public App(IContainer container)
        {
            this.container = container;

            var vmm = container.Resolve<IViewModelManager>();
            var detail = vmm.CreatePage<Samples.ViewModels.Le.ScanViewModel>();
			var master = vmm.CreatePage<MenuViewModel>();

			this.MainPage = new MasterDetailPage
			{
				Master = master,
				Detail = new NavigationPage(detail)
			};
        }


        protected override void OnResume()
        {
            base.OnResume();
            this.container.Resolve<IEnumerable<IAppLifecycle>>().Each(x => x.OnForeground());
        }


        protected override void OnSleep()
        {
            base.OnSleep();
            this.container.Resolve<IEnumerable<IAppLifecycle>>().Each(x => x.OnBackground());
        }
    }
}