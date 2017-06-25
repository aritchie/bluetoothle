using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Samples.ViewModels;
using Xamarin.Forms;


namespace Samples.Services.Impl
{
    public class ViewModelManagerImpl : IViewModelManager
    {
        readonly ILifetimeScope scope;


        public ViewModelManagerImpl(ILifetimeScope scope)
        {
            this.scope = scope;
        }


        public TViewModel Create<TViewModel>(object args = null) where TViewModel : class, IViewModel
        {
            Debug.WriteLine("Resolving " + typeof(TViewModel).FullName);
            var vm = this.scope.Resolve<TViewModel>();
            vm.Init(args);
            return vm;
        }


        public async Task Push<TViewModel>(object args = null) where TViewModel : class, IViewModel
        {
            var page = this.CreatePage<TViewModel>(args);
            var nav = this.GetCurrentNav();
            await nav.PushAsync(page);
        }


        public Task Pop() => this.GetCurrentNav().PopAsync(true);


		public Page CreatePage<TViewModel>(object args) where TViewModel : class, IViewModel
		{
			var viewModel = this.Create<TViewModel>(args);
			var pageTypeName = viewModel
				.GetType()
				.FullName
				.Replace("ViewModel", "Page");

			var pageType = Type.GetType(pageTypeName);
			if (pageType == null)
				throw new ArgumentException("No corresponding page found for viewmodel");

			var page = Activator.CreateInstance(pageType) as Page;
			//var page = this.scope.Resolve(pageType) as Page;
			if (page == null)
				throw new ArgumentException("No page resolved for " + pageTypeName);

			page.BindingContext = viewModel;
			return page;
		}


        INavigation GetCurrentNav()
        {
            var nav = Application.Current.MainPage as NavigationPage;
            if (nav == null)
                throw new ArgumentException("Top page should be tabs");

            //var nav = tabs.CurrentPage as NavigationPage;
            //if (nav == null)
                //throw new ArgumentException("Current tab is not a navpage");

            //return nav.Navigation;
            return nav.Navigation;
        }
    }
}
