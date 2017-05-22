using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Plugin.BluetoothLE;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;


namespace Samples.ViewModels.Le
{
    public class AdapterListViewModel : AbstractViewModel
    {
        readonly IAdapterScanner scanner;


        public AdapterListViewModel(IAdapterScanner scanner, IViewModelManager vmManager)
        {
            this.scanner = scanner;
            this.Select = ReactiveCommand.CreateFromTask<IAdapter>(async adapter =>
            {
                CrossBleAdapter.Current = adapter;
                await vmManager.Push<MainViewModel>();
            });
            this.Scan = ReactiveCommand.Create(() =>
            {
                this.IsBusy = true;
                this.scanner
                    .FindAdapters()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(
                        this.Adapters.Add,
                        () => this.IsBusy = false
                    );
            },
            this.WhenAny(x => x.IsBusy, x => !x.Value));
        }


        public override void OnActivate()
        {
            base.OnActivate();
            this.Scan.Execute(null);
        }


        public ObservableList<IAdapter> Adapters { get; } = new ObservableList<IAdapter>();
        public ICommand Select { get; }
        public ICommand Scan { get; }
        [Reactive] public bool IsBusy { get; private set; }
    }
}
