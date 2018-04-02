using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Plugin.BluetoothLE;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Ble
{
    public class AdapterListViewModel : ViewModel
    {
        readonly IAdapterScanner scanner;


        public AdapterListViewModel()
        {
            this.Select = ReactiveCommand.CreateFromTask<IAdapter>(async adapter =>
            {
                CrossBleAdapter.Current = adapter;
                await App.Current.MainPage.Navigation.PushAsync(new AdapterPage());
            });
            this.Scan = ReactiveCommand.Create(() =>
            {
                this.IsBusy = true;
                CrossBleAdapter
                    .AdapterScanner
                    .FindAdapters()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(
                        this.Adapters.Add,
                        async () =>
                        {
                            this.IsBusy = false;
                            switch (this.Adapters.Count)
                            {
                                case 0:
                                    UserDialogs.Instance.Alert("No BluetoothLE Adapters Found");
                                    break;

                                case 1:
                                    CrossBleAdapter.Current = this.Adapters.First();
                                    await App.Current.MainPage.Navigation.PushAsync(new AdapterPage());
                                    break;
                            }
                        }
                    );
            },
            this.WhenAny(x => x.IsBusy, x => !x.Value));
        }


        public override void OnActivated()
        {
            base.OnActivated();
            this.Scan.Execute(null);
        }


        public ObservableCollection<IAdapter> Adapters { get; } = new ObservableCollection<IAdapter>();
        public ICommand Select { get; }
        public ICommand Scan { get; }


        bool busy;
        public bool IsBusy
        {
            get => this.busy;
            private set => this.RaiseAndSetIfChanged(ref this.busy, value);
        }
    }
}
