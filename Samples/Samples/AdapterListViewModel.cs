using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using Prism.Navigation;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples
{
    public class AdapterListViewModel : ViewModel
    {
        readonly IAdapterScanner adapterScanner;
        readonly INavigationService navigationService;


        public AdapterListViewModel(INavigationService navigationService,
                                    IAdapterScanner adapterScanner,
                                    IUserDialogs dialogs)
        {
            this.adapterScanner = adapterScanner;
            this.navigationService = navigationService;

            this.Select = ReactiveCommand.CreateFromTask<IAdapter>(navigationService.NavToAdapter);

            this.Scan = ReactiveCommand.Create(() =>
            {
                this.IsBusy = true;
                adapterScanner
                    .FindAdapters()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(
                        this.Adapters.Add,
                        ex => dialogs.Alert(ex.ToString(), "Error"),
                        async () =>
                        {
                            this.IsBusy = false;
                            switch (this.Adapters.Count)
                            {
                                case 0:
                                    dialogs.Alert("No BluetoothLE Adapters Found");
                                    break;

                                case 1:
                                    var adapter = this.Adapters.First();
                                    await navigationService.NavToAdapter(adapter);
                                    break;
                            }
                        }
                    );
            },
            this.WhenAny(x => x.IsBusy, x => !x.Value));
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            if (this.adapterScanner.IsSupported)
            {
                this.Scan.Execute(null);
            }
            else
            {
                this.navigationService.NavToAdapter(CrossBleAdapter.Current);
            }
        }


        public ObservableCollection<IAdapter> Adapters { get; } = new ObservableCollection<IAdapter>();
        public ICommand Select { get; }
        public ICommand Scan { get; }
    }
}
