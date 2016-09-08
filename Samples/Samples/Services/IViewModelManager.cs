using System;
using System.Threading.Tasks;
using Samples.ViewModels;
using Xamarin.Forms;


namespace Samples.Services
{
    public interface IViewModelManager
    {
        TViewModel Create<TViewModel>(object args = null) where TViewModel : class, IViewModel;
		Page CreatePage<TViewModel>(object args = null) where TViewModel : class, IViewModel;
		Task Push<TViewModel>(object args = null) where TViewModel : class, IViewModel;
        Task Pop();

		void SetDetail<TViewModel>(object args = null) where TViewModel : class, IViewModel;
        //ICommand PushCommand<TViewModel>(Func<object> funcArgs = null) where TViewModel : class, IViewModel;
    }
}
